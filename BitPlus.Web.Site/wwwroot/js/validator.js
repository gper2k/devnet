import ValidResult from "./validresult.js";
/** 필터 키(대소문자/별칭 무관) → 서버 리소스 메시지 키 매핑. BitPlus.Web/Security/Validation/Filters/* 와 1:1 대응. */
const messageKeyMap = {
    required: "ValidRequired",
    minlength: "ValidMinLength", minlen: "ValidMinLength",
    maxlength: "ValidMaxLength", maxlen: "ValidMaxLength",
    minvalue: "ValidMinValue", min: "ValidMinValue", minimum: "ValidMinValue",
    maxvalue: "ValidMaxValue", max: "ValidMaxValue", maximum: "ValidMaxValue",
    mindate: "ValidMinDate",
    maxdate: "ValidMaxDate",
    mintime: "ValidMinTime",
    maxtime: "ValidMaxTime",
    mindatetime: "ValidMinDateTime",
    maxdatetime: "ValidMaxDateTime",
    email: "ValidEmail", mail: "ValidEmail",
    domain: "ValidDomain", host: "ValidDomain",
    regex: "ValidRegex", pattern: "ValidRegex",
};
/** "today"/"now" 키워드와 명시적 포맷을 함께 지원하는 날짜 파서 (서버 DateFilterBase.ParseDate 동등). */
function parseDateOnly(raw) {
    if (raw === null || raw === undefined)
        return null;
    const s = String(raw).trim();
    if (s === "")
        return null;
    if (/^(today|now)$/i.test(s)) {
        const d = new Date();
        d.setHours(0, 0, 0, 0);
        return d;
    }
    const d = new Date(s);
    if (isNaN(d.getTime()))
        return null;
    d.setHours(0, 0, 0, 0);
    return d;
}
/** "now" 키워드와 HH:mm[:ss] 포맷을 지원하는 시간 파서 (서버 TimeFilterBase.ParseTime 동등, 분 단위까지 비교). */
function parseTimeOnly(raw) {
    if (raw === null || raw === undefined)
        return null;
    const s = String(raw).trim();
    if (s === "")
        return null;
    if (/^now$/i.test(s)) {
        const n = new Date();
        return n.getHours() * 60 + n.getMinutes();
    }
    const m = s.match(/^(\d{1,2}):(\d{2})(?::(\d{2}))?$/);
    if (m)
        return parseInt(m[1], 10) * 60 + parseInt(m[2], 10);
    const d = new Date(s);
    if (!isNaN(d.getTime()))
        return d.getHours() * 60 + d.getMinutes();
    return null;
}
/** "today"/"now" 키워드를 지원하는 일시 파서 (서버 DateTimeFilterBase.ParseDateTime 동등). */
function parseDateTime(raw) {
    if (raw === null || raw === undefined)
        return null;
    const s = String(raw).trim();
    if (s === "")
        return null;
    if (/^now$/i.test(s))
        return new Date();
    if (/^today$/i.test(s)) {
        const d = new Date();
        d.setHours(0, 0, 0, 0);
        return d;
    }
    const d = new Date(s);
    return isNaN(d.getTime()) ? null : d;
}
function isEmpty(value) {
    return value === null || value === undefined || (typeof value === "string" && value.trim() === "");
}
function getLength(value) {
    if (value === null || value === undefined)
        return null;
    if (typeof value === "string")
        return value.length;
    if (Array.isArray(value))
        return value.length;
    return null;
}
/** 필터 키(별칭 포함) → 검증 함수. 전부 서버 Filters/*.cs 와 동일한 통과/실패 기준을 따른다. */
const filters = {
    required: (value) => {
        if (value === null || value === undefined)
            return false;
        if (typeof value === "string")
            return value.trim() !== "";
        if (Array.isArray(value))
            return value.length > 0;
        return true;
    },
    minlength: (value, args) => { const min = Number(args?.[0] ?? 0); if (min <= 0 || isEmpty(value))
        return true; const len = getLength(value); return len === null ? true : len >= min; },
    maxlength: (value, args) => { const max = Number(args?.[0] ?? 0); if (max <= 0 || isEmpty(value))
        return true; const len = getLength(value); return len === null ? true : len <= max; },
    minvalue: (value, args) => { if (args?.[0] === undefined || args?.[0] === null || isEmpty(value))
        return true; const n = Number(value); return isNaN(n) ? false : n >= Number(args[0]); },
    maxvalue: (value, args) => { if (args?.[0] === undefined || args?.[0] === null || isEmpty(value))
        return true; const n = Number(value); return isNaN(n) ? false : n <= Number(args[0]); },
    mindate: (value, args) => { const min = parseDateOnly(args?.[0]); if (!min || isEmpty(value))
        return true; const d = parseDateOnly(value); return d === null ? false : d.getTime() >= min.getTime(); },
    maxdate: (value, args) => { const max = parseDateOnly(args?.[0]); if (!max || isEmpty(value))
        return true; const d = parseDateOnly(value); return d === null ? false : d.getTime() <= max.getTime(); },
    mintime: (value, args) => { const min = parseTimeOnly(args?.[0]); if (min === null || isEmpty(value))
        return true; const t = parseTimeOnly(value); return t === null ? false : t >= min; },
    maxtime: (value, args) => { const max = parseTimeOnly(args?.[0]); if (max === null || isEmpty(value))
        return true; const t = parseTimeOnly(value); return t === null ? false : t <= max; },
    mindatetime: (value, args) => { const min = parseDateTime(args?.[0]); if (!min || isEmpty(value))
        return true; const d = parseDateTime(value); return d === null ? false : d.getTime() >= min.getTime(); },
    maxdatetime: (value, args) => { const max = parseDateTime(args?.[0]); if (!max || isEmpty(value))
        return true; const d = parseDateTime(value); return d === null ? false : d.getTime() <= max.getTime(); },
    email: (value) => { if (isEmpty(value))
        return true; const s = String(value); if (s.length > 254)
        return false; return /^[a-zA-Z0-9.!#$%&'*+/=?^_`{|}~-]+@[a-zA-Z0-9](?:[a-zA-Z0-9-]{0,61}[a-zA-Z0-9])?(?:\.[a-zA-Z0-9](?:[a-zA-Z0-9-]{0,61}[a-zA-Z0-9])?)+$/.test(s); },
    domain: (value, args) => {
        if (isEmpty(value))
            return true;
        const plain = /^([\w-]+\.)*[\w-]+(:\d+)?\/?$/;
        const withProtocol = /^https?:\/\/([\w-]+\.)*[\w-]+(:\d+)?\/?$/;
        const optionalProtocol = /^(https?:\/\/)?([\w-]+\.)*[\w-]+(:\d+)?\/?$/;
        const mode = args?.[0];
        const pattern = mode === true ? optionalProtocol
            : typeof mode === "string" && ["protocol", "requireprotocol", "withprotocol"].includes(mode.toLowerCase()) ? withProtocol
                : typeof mode === "string" && ["allowprotocol", "optionalprotocol", "any", "all", "true"].includes(mode.toLowerCase()) ? optionalProtocol
                    : plain;
        return pattern.test(String(value));
    },
    regex: (value, args) => { if (isEmpty(value))
        return true; const pattern = args?.[0]; if (!pattern)
        return true; try {
        return new RegExp(pattern).test(String(value));
    }
    catch {
        return true;
    } },
};
// 별칭을 동일 함수로 연결 (서버 ValidAliasAttribute 와 동일한 별칭 목록)
filters.minlen = filters.minlength;
filters.maxlen = filters.maxlength;
filters.min = filters.minvalue;
filters.minimum = filters.minvalue;
filters.max = filters.maxvalue;
filters.maximum = filters.maxvalue;
filters.mail = filters.email;
filters.host = filters.domain;
filters.pattern = filters.regex;
/**
 * 서버와 동일한 ValidConfigs/*.json 정의를 그대로 재사용하는 클라이언트 측 유효성 검사기.
 * - 서버: ValidAttribute(IActionFilter) 가 요청 실행 전 검증
 * - 클라이언트: 이 클래스가 submit 직전 같은 JSON 으로 재검증 (이중 적용, 스키마 단일 소스)
 */
export default class Validator {
    fields;
    constructor(fields) {
        this.fields = fields ?? [];
    }
    /**
     * 주어진 값 맵({필드명: 값})을 전체 필드 규칙에 대해 검증한다.
     * container 를 주면 같은 name 의 입력항에 data-min / data-max 가 있을 때 해당 필드의 minLength / maxLength 를 그 값으로 대체한다
     * (예: 설정값에 따라 달라지는 최소 길이를 서버 렌더링 시 data-min 으로 내려 검증에 반영).
     * @param values 필드명(ValidConfig.name) → 값 맵
     * @param container 입력항을 찾을 영역 (선택)
     */
    async valid(values, container) {
        const messages = {};
        for (const rule of this.fields) {
            const field = Validator.applyInputLimits(rule, container);
            const value = values[field.name];
            const ok = field.converter
                ? await this.validConverter(field, value)
                : this.validFilters(field, value);
            if (!ok.success)
                messages[field.name] = await this.getMsg(field, ok.failedKey, value);
        }
        return new ValidResult(Object.keys(messages).length === 0, messages);
    }
    /** `valid`의 별칭 (서버 ValidAttribute 네이밍과 맞춤) */
    validAsync = (values, container) => this.valid(values, container);
    /**
     * 같은 name 의 입력항 data-min / data-max 값으로 길이 필터를 대체한 필드 규칙을 만든다 (원본 규칙은 변경하지 않음).
     * @param field 필드 규칙
     * @param container 입력항을 찾을 영역
     * @returns 적용된 필드 규칙
     */
    static applyInputLimits(field, container) {
        const input = container?.querySelector(`[name="${field.name}"]`);
        const min = input?.dataset.min;
        const max = input?.dataset.max;
        if (!min && !max)
            return field;
        const filters = { ...(field.filters ?? {}) };
        if (min)
            filters["minLength"] = [Number(min)];
        if (max)
            filters["maxLength"] = [Number(max)];
        return { ...field, filters };
    }
    validFilters(field, value) {
        for (const key of Object.keys(field.filters ?? {})) {
            const fn = filters[key] ?? filters[key.toLowerCase()];
            if (!fn)
                continue;
            if (!fn(value, field.filters[key]))
                return { success: false, failedKey: key };
        }
        return { success: true };
    }
    /**
     * converter 필드 처리 — 현재는 "language"(다국어 {culture: text} 맵) 만 지원.
     * required 는 하나라도 채워져 있으면 통과, 그 외 필터는 채워진 문화권 전부가 통과해야 한다.
     */
    async validConverter(field, value) {
        if (field.converter !== "language")
            return this.validFilters(field, value);
        let map = {};
        try {
            map = typeof value === "string" ? JSON.parse(value) : (value ?? {});
        }
        catch {
            map = {};
        }
        const texts = Object.values(map).filter(t => !isEmpty(t));
        for (const key of Object.keys(field.filters ?? {})) {
            const fn = filters[key] ?? filters[key.toLowerCase()];
            if (!fn)
                continue;
            const pass = key.toLowerCase() === "required"
                ? texts.length > 0
                : texts.every(t => fn(t, field.filters[key]));
            if (!pass)
                return { success: false, failedKey: key };
        }
        return { success: true };
    }
    async getMsg(field, filterKey, value) {
        const dpName = await this.resolveDisplayName(field.displayName);
        const msgKey = messageKeyMap[filterKey.toLowerCase()] ?? "ValidationFailed";
        const langs = await common.getLangsAsync(`${msgKey}`);
        const template = langs[msgKey] ?? msgKey;
        const limit = field.filters?.[filterKey]?.[0];
        return template.replace("{0}", dpName).replace("{1}", limit !== undefined ? String(limit) : "");
    }
    async resolveDisplayName(displayName) {
        if (!displayName)
            return "";
        if (!displayName.startsWith("@"))
            return displayName;
        const key = displayName.slice(1);
        const langs = await common.getLangsAsync(key);
        return langs[key] ?? key;
    }
}
// classic(non-module) 인라인 페이지 스크립트(예: 탭 기반 뷰어로 로드되는 조각 페이지의 초기화 코드)에서도
// import 없이 바로 `new Validator(...)` 로 쓸 수 있도록 전역에 노출한다.
window.Validator = Validator;
//# sourceMappingURL=validator.js.map