using System.Text.Json;

namespace BitPlus.Web.Site.Middlewares;

/// <summary>
/// 일간 페이지 요청 수·방문자 수를 업로드 폴더 아래 파일로 집계합니다 (DB 를 쓰지 않음).
/// 일별 Counter/{yyyyMM}/{dd}.json, 월 모음 Counter/{yyyyMM}/month.json(그날 이전 날별 기록을 한 파일에 — {"01": {pages, visitors}, ...}), 연 모음 Counter/{yyyy}.json(끝난 달별 합 — {"01": {...}, ...}).
/// 날짜는 서버 시각의 날짜(시각은 보지 않음). 그날 첫 요청에서 그날 파일을 만들면서 월 모음을 다시 쓰고, 달의 첫 파일이면 지난달 모음을 마감해 연 모음도 다시 씁니다.
/// 오늘 값은 메모리에 모았다가 10초마다·종료 시 파일에 씁니다.
/// ponytail: 서버 한 대 기준(프로세스 안 잠금) — 여러 대로 늘리면 Redis 카운터 등으로 옮길 것.
/// </summary>
public class VisitCounter : IDisposable
{
    /// <summary>업로드 폴더 아래 카운터 폴더 이름</summary>
    public const string FolderName = "Counter";

    /// <summary>월 모음 파일 이름</summary>
    public const string MonthFileName = "month.json";

    /// <summary>파일에 쓰는 간격</summary>
    private static readonly TimeSpan FlushGap = TimeSpan.FromSeconds(10);

    /// <summary>카운터 파일 JSON 형식 (camelCase)</summary>
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly Lock sync;
    private readonly string root;
    private readonly ILogger<VisitCounter> logger;
    private readonly Timer timer;
    private DateOnly day;
    private VisitCount today;
    private bool dirty;

    /// <summary>
    /// 업로드 폴더의 카운터 폴더를 쓰는 집계기를 만듭니다.
    /// </summary>
    /// <param name="config">웹 설정 (업로드 폴더)</param>
    /// <param name="logger">파일 쓰기 실패 기록용</param>
    public VisitCounter(WebConfig config, ILogger<VisitCounter> logger) : this(Path.Combine(config.UploadFolder, FolderName), logger)
    {
    }

    /// <summary>
    /// 지정 폴더를 쓰는 집계기를 만듭니다.
    /// </summary>
    /// <param name="root">카운터 폴더 경로</param>
    /// <param name="logger">파일 쓰기 실패 기록용</param>
    public VisitCounter(string root, ILogger<VisitCounter> logger)
    {
        this.sync = new Lock();
        this.root = root;
        this.logger = logger;
        this.day = DateOnly.MinValue;
        this.today = new VisitCount();
        this.dirty = false;
        this.timer = new Timer(_ => this.Flush(), null, FlushGap, FlushGap);
    }

    /// <summary>오늘 날짜 (서버 시각 기준)</summary>
    public static DateOnly Today => DateOnly.FromDateTime(DateTime.Now);

    /// <summary>
    /// 페이지 요청 한 건을 셉니다.
    /// </summary>
    /// <param name="newVisitor">그날 처음 온 방문자인지 (하루 쿠키를 새로 발급했는지)</param>
    public void Hit(bool newVisitor) => this.Hit(newVisitor, Today);

    /// <summary>
    /// 지정 날짜의 페이지 요청 한 건을 셉니다 (날짜 넘김 확인용 — 화면 요청은 <see cref="Hit(bool)"/>).
    /// </summary>
    /// <param name="newVisitor">그날 처음 온 방문자인지</param>
    /// <param name="date">집계 날짜</param>
    public void Hit(bool newVisitor, DateOnly date)
    {
        try
        {
            lock (this.sync)
            {
                this.Open(date);
                this.today.Pages++;
                if (newVisitor) this.today.Visitors++;
                this.dirty = true;
            }
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
            this.logger.LogWarning(e, "Visit counter write failed");
        }
    }

    /// <summary>
    /// 메모리에 모은 오늘 값을 그날 파일에 씁니다 (바뀐 것이 없으면 쓰지 않음).
    /// </summary>
    public void Flush()
    {
        try
        {
            lock (this.sync)
            {
                if (!this.dirty) return;
                Write(this.DayFile(this.day), this.today);
                this.dirty = false;
            }
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
            this.logger.LogWarning(e, "Visit counter flush failed");
        }
    }

    /// <summary>
    /// 지정 날짜의 값을 읽습니다 (오늘 집계 중이면 메모리 값).
    /// </summary>
    /// <param name="date">날짜</param>
    /// <returns>그날 페이지 요청 수·방문자 수 (기록이 없으면 0)</returns>
    public VisitCount Day(DateOnly date)
    {
        lock (this.sync)
        {
            if (date == this.day)
            {
                var copy = new VisitCount();
                copy.Add(this.today);
                return copy;
            }
        }
        return Read<VisitCount>(this.DayFile(date));
    }

    /// <summary>
    /// 그 달의 합을 읽습니다: 월 모음 파일의 날별 기록 + 모음에 아직 없는 날 파일(오늘 등).
    /// </summary>
    /// <param name="date">그 달의 아무 날짜</param>
    /// <returns>그 달 합</returns>
    public VisitCount Month(DateOnly date)
    {
        var days = Read<SortedDictionary<string, VisitCount>>(this.MonthFile(date));
        var sum = new VisitCount();
        foreach (var count in days.Values) sum.Add(count);
        foreach (var day in this.Days(date).Keys.Where(p => !days.ContainsKey($"{p:00}"))) sum.Add(this.Day(new DateOnly(date.Year, date.Month, day)));
        return sum;
    }

    /// <summary>
    /// 그 해의 합을 읽습니다: 연 모음 파일의 달별 합 + 모음에 아직 없는 달(이번 달 등)은 <see cref="Month"/>.
    /// </summary>
    /// <param name="year">연도</param>
    /// <returns>그 해 합</returns>
    public VisitCount Year(int year)
    {
        var months = Read<SortedDictionary<string, VisitCount>>(this.YearFile(year));
        var sum = new VisitCount();
        for (var month = 1; month <= 12; month++)
        {
            if (months.TryGetValue($"{month:00}", out var count)) sum.Add(count);
            else if (Directory.Exists(this.MonthFolder(new DateOnly(year, month, 1)))) sum.Add(this.Month(new DateOnly(year, month, 1)));
        }
        return sum;
    }

    /// <summary>
    /// 남은 값을 쓰고 타이머를 멈춥니다.
    /// </summary>
    public void Dispose()
    {
        this.timer.Dispose();
        this.Flush();
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// 집계 날짜를 맞춥니다: 날이 바뀌면 전날 값을 쓰고, 그날 파일이 있으면 읽고 없으면 모음 파일을 다시 쓴 뒤 새로 만듭니다.
    /// </summary>
    /// <param name="date">오늘 날짜</param>
    private void Open(DateOnly date)
    {
        if (date == this.day) return;
        this.Flush();
        var file = this.DayFile(date);
        if (File.Exists(file)) this.today = Read<VisitCount>(file);
        else
        {
            this.Rollover(date);
            this.today = new VisitCount();
            Write(file, this.today);
        }
        this.day = date;
        this.dirty = false;
    }

    /// <summary>
    /// 그날 파일을 만들기 전 모음 파일을 다시 씁니다: 이달 모음 = 이달의 그날 이전 날별 기록, 이달 첫 파일이면 지난달 모음(마감)과 그 해의 연 모음(월별 합).
    /// </summary>
    /// <param name="date">새로 시작하는 날짜</param>
    private void Rollover(DateOnly date)
    {
        var firstOfMonth = this.Days(date).Count == 0;
        Write(this.MonthFile(date), this.CollectDays(date, date.Day));
        if (!firstOfMonth) return;

        var last = date.AddMonths(-1);
        if (!Directory.Exists(this.MonthFolder(last))) return;
        Write(this.MonthFile(last), this.CollectDays(last, int.MaxValue));
        Write(this.YearFile(last.Year), this.CollectMonths(last.Year, last.Month));
    }

    /// <summary>이달 날짜 파일들 (일 → 파일 경로)</summary>
    private Dictionary<int, string> Days(DateOnly date)
    {
        var folder = this.MonthFolder(date);
        if (!Directory.Exists(folder)) return [];
        return Directory.EnumerateFiles(folder, "*.json")
            .Select(p => (Day: int.TryParse(Path.GetFileNameWithoutExtension(p), out var d) ? d : 0, Path: p))
            .Where(p => p.Day > 0).ToDictionary(p => p.Day, p => p.Path);
    }

    /// <summary>이달의 지정 일 이전 날별 기록 모음 ("dd" → 그날 값)</summary>
    private SortedDictionary<string, VisitCount> CollectDays(DateOnly date, int beforeDay)
        => new(this.Days(date).Where(p => p.Key < beforeDay).ToDictionary(p => $"{p.Key:00}", p => Read<VisitCount>(p.Value)));

    /// <summary>그 해 1월부터 지정 월까지의 월별 합 모음 ("MM" → 그 달 날별 기록의 합)</summary>
    private SortedDictionary<string, VisitCount> CollectMonths(int year, int throughMonth)
    {
        var months = new SortedDictionary<string, VisitCount>();
        for (var month = 1; month <= throughMonth; month++)
        {
            var file = this.MonthFile(new DateOnly(year, month, 1));
            if (!File.Exists(file)) continue;
            var sum = new VisitCount();
            foreach (var count in Read<SortedDictionary<string, VisitCount>>(file).Values) sum.Add(count);
            months[$"{month:00}"] = sum;
        }
        return months;
    }

    private string MonthFolder(DateOnly date) => Path.Combine(this.root, date.ToString("yyyyMM"));

    private string DayFile(DateOnly date) => Path.Combine(this.MonthFolder(date), $"{date.Day:00}.json");

    private string MonthFile(DateOnly date) => Path.Combine(this.MonthFolder(date), MonthFileName);

    private string YearFile(int year) => Path.Combine(this.root, $"{year}.json");

    /// <summary>카운터 파일을 읽습니다 (없으면 빈 값).</summary>
    private static T Read<T>(string file) where T : new()
        => File.Exists(file) ? JsonSerializer.Deserialize<T>(File.ReadAllText(file), JsonOptions) ?? new T() : new T();

    /// <summary>카운터 파일을 씁니다 (임시 파일에 쓴 뒤 바꿔 끼워 쓰다 끊겨도 깨지지 않게).</summary>
    private static void Write<T>(string file, T value)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(file)!);
        var temp = file + ".tmp";
        File.WriteAllText(temp, JsonSerializer.Serialize(value, JsonOptions));
        File.Move(temp, file, true);
    }
}
