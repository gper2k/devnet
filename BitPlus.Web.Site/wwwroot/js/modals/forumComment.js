/**
 * 포럼 댓글 입력 모달: common.modal.append("forumComment", args) 로 열며 토픽 댓글 쓰기·1단 댓글에 댓글 달기·댓글 수정에 쓴다.
 * 에디터(본문)와 첨부 파일을 두고, 저장하면 저장한 댓글의 글번호, 닫으면 null 로 닫힌다. 임시 저장은 없어 닫으면 쓰던 내용을 버리며(내용이 있으면 확인 한 번), 올린 첨부는 서버가 나중에 정리한다.
 */
export default class ForumComment {
    langs;
    /**
     * ForumComment 기본 생성자
     */
    constructor() {
        this.langs = {};
    }
    /**
     * 문구 다국어 사전 로드
     */
    async loadAsync(args) {
        this.langs = await window["common"].getLangsAsync("Cancel, Save, Comment, CommentReply, CommentDiscardMsg, CommentOnParent, FileSelect, FileUploadDone, FileUploadFailed, AllowExtensions, AllowCount, AllowSize, AllowTotalSize, FileTypeNotAllowed, FileSizeOver, FileCountOver, FileTotalSizeOver");
    }
    /**
     * 대화상자 DOM 생성 (제목 + 에디터 + 첨부 + 취소/저장)
     */
    render(args) {
        const common = window["common"];
        const t = this.langs;
        const a = args;
        const editNo = a.commentNo ?? 0, parentNo = a.parentNo ?? 0;
        const ownerKey = editNo > 0 ? a.id : crypto.randomUUID(); // 새 댓글은 모달이 만든 식별키가 댓글의 식별키가 되고 첨부·에디터 이미지의 주인
        const diag = document.createElement("dialog");
        diag.className = "forumComment";
        diag.innerHTML = `
            <header><i class="fi fiComment"></i> <span></span></header>
            <div>
                <textarea name="body"></textarea>
                <section hidden>
                    <div><span><button type="button" tabindex="-1"><i class="fi fiAttach"></i> ${t["FileSelect"]}</button><input type="file" multiple /></span><small></small></div>
                    <ul></ul>
                </section>
            </div>
            <footer>
                <button type="button"><i class="fi fiProhibit"></i> ${t["Cancel"]}</button>
                <button type="button" class="primary"><i class="fi fiSave"></i> ${t["Save"]}</button>
            </footer>
        `;
        diag.querySelector(":scope > header > span").textContent = editNo > 0 ? t["Comment"] : parentNo > 0 ? t["CommentOnParent"].replace("{0}", a.parentWriter ?? "") : t["CommentReply"];
        const textarea = diag.querySelector("textarea");
        const box = diag.querySelector("section"), fileList = box.querySelector("ul"), fileInput = box.querySelector("input[type=file]");
        const [cancelBtn, saveBtn] = diag.querySelectorAll(":scope > footer > button");
        const files = (a.files ?? []).map(p => ({ ...p, state: "done", percent: 100, registered: true }));
        const removed = []; // 저장할 때 지울 등록된 첨부
        let editor = null, ruleInfo = null;
        const owner = () => editNo > 0 ? { commentNo: editNo } : { commentTopic: a.topicNo, commentParent: parentNo, draftKey: ownerKey }; // 올릴 곳: 수정은 그 댓글, 새 댓글은 식별키의 임시 글
        const fileText = (f) => f.state === "done" ? t["FileUploadDone"] : f.state === "error" ? (f.text ?? "") : f.percent + "%";
        const paint = (f) => {
            const li = fileList.children[files.indexOf(f)];
            if (!li)
                return;
            li.dataset["state"] = f.state;
            li.querySelector("progress").value = f.percent;
            li.querySelector("em").textContent = fileText(f);
        };
        const bind = () => common.bindList(fileList, files, (f) => common.html `
            <li data-state="${f.state}">
                <span title="${f.name}">${f.name}</span>
                <div><progress max="100" value="${f.percent}"></progress><em>${fileText(f)}</em></div>
                <small>${common.fileSizeText(f.size)}</small>
                <button type="button" class="danger"><i class="fi fiDismiss"></i></button>
            </li>`);
        const upload = async (f) => {
            f.ctrl = new AbortController();
            const res = await common.uploadFile("/api/content/file", f.file, { ruleNo: a.ruleNo, removeFiles: JSON.stringify(removed), ...owner() }, (percent) => { f.percent = percent; paint(f); }, f.ctrl.signal);
            if (f.ctrl.signal.aborted)
                return; // 지우기로 취소됨
            if (res?.success)
                Object.assign(f, { id: res.data[0].id, state: "done", percent: 100, file: null });
            else
                Object.assign(f, { state: "error", text: res?.message ?? t["FileUploadFailed"] });
            paint(f);
        };
        /** 쓴 내용이 있는지 (글자, 이미지, 첨부) */
        const hasContent = () => !!editor && (editor.getHtml().replace(/<(?!img)[^>]*>|&nbsp;/gi, "").trim().length > 0 || files.length > 0);
        /** 닫기: 쓴 내용이 있으면 버릴지 한 번 확인 */
        const tryClose = async () => {
            if (hasContent() && !await common.confirm(t["CommentDiscardMsg"]))
                return;
            diag["closer"](null);
        };
        cancelBtn.addEventListener("click", tryClose);
        diag.addEventListener("cancel", e => { e.preventDefault(); void tryClose(); }); // Esc
        saveBtn.addEventListener("click", async () => {
            if (!editor || files.some(f => f.state === "uploading"))
                return;
            const body = editor.getHtml();
            if (!/\S/.test(body.replace(/<(?!img)[^>]*>|&nbsp;/gi, "")))
                return editor.focus();
            const res = await common.postSend("/api/content/comment", common.makeFormData({ ruleNo: a.ruleNo, contentNo: a.topicNo, parentNo, body, removeFiles: JSON.stringify(removed), ...owner() }));
            if (res?.success)
                diag["closer"](res.data.contentNo || true); // 저장한 댓글의 글번호 (보는 쪽이 그 댓글로 이동)
        });
        void (async () => {
            textarea.value = a.body ?? "";
            // 보통 화면은 고정 높이(49.5em), 작은 화면(720px 이하)은 대화상자가 화면의 99% 라서 남는 높이를 에디터가 채움
            const small = window.matchMedia("(max-width: 720px)").matches;
            editor = await common.makeEditor(textarea, { ownerType: a.ownerType, ownerKey, siteCss: true, height: small ? "100%" : "49.5em" });
            ruleInfo = (await common.getSend(`/api/content/file?ruleNo=${a.ruleNo}`, { useLoading: false }))?.data;
            if (!ruleInfo?.enabled || !ruleInfo.canUpload)
                return;
            box.hidden = false;
            box.querySelector("small").textContent = [
                ruleInfo.extensions.length ? `${t["AllowExtensions"]}: ${ruleInfo.extensions.join(", ")}` : "",
                ruleInfo.count > 0 ? `${t["AllowCount"]}: ${ruleInfo.count}` : "",
                ruleInfo.size > 0 ? `${t["AllowSize"]}: ${common.fileSizeText(ruleInfo.size)}` : "",
                ruleInfo.totalSize > 0 ? `${t["AllowTotalSize"]}: ${common.fileSizeText(ruleInfo.totalSize)}` : ""
            ].filter(Boolean).join(" · ");
            if (ruleInfo.extensions.length)
                fileInput.accept = ruleInfo.extensions.map((e) => "." + e).join(",");
            fileInput.onchange = async () => {
                const picked = [...fileInput.files];
                fileInput.value = "";
                // 서버가 같은 검사를 하지만 큰 파일을 올리기 전에 먼저 알려 준다: 하나라도 어긋나면 아무것도 올리지 않는다
                let count = files.length, total = files.reduce((sum, p) => sum + p.size, 0);
                for (const file of picked) {
                    const ext = file.name.includes(".") ? file.name.split(".").pop().toLowerCase() : "";
                    if (ruleInfo.extensions.length && !ruleInfo.extensions.includes(ext))
                        return common.alert(t["FileTypeNotAllowed"].replace("{0}", file.name));
                    if (ruleInfo.size > 0 && file.size > ruleInfo.size)
                        return common.alert(t["FileSizeOver"].replace("{0}", file.name));
                    count++;
                    total += file.size;
                }
                if (ruleInfo.count > 0 && count > ruleInfo.count)
                    return common.alert(t["FileCountOver"].replace("{0}", ruleInfo.count));
                if (ruleInfo.totalSize > 0 && total > ruleInfo.totalSize)
                    return common.alert(t["FileTotalSizeOver"]);
                const added = picked.map(file => ({ id: null, name: file.name, size: file.size, state: "uploading", percent: 0, registered: false, file }));
                files.push(...added);
                bind();
                for (const f of added)
                    if (files.includes(f))
                        await upload(f); // 하나씩 차례로 (개수·용량 검사가 엇갈리지 않게)
            };
            fileList.onclick = async (e) => {
                const li = e.target.closest("li");
                if (!e.target.closest("button") || !li)
                    return;
                const f = files[[...fileList.children].indexOf(li)];
                if (f.state === "uploading")
                    f.ctrl?.abort();
                else if (f.registered)
                    removed.push(f.id);
                else if (f.state === "done") {
                    const res = await common.postSend("/api/content/file/delete", common.makeFormData({ ruleNo: a.ruleNo, fileId: f.id, ...owner() }), { useLoading: false });
                    if (!res?.success)
                        return;
                }
                files.splice(files.indexOf(f), 1);
                bind();
            };
            bind();
        })();
        return diag;
    }
}
//# sourceMappingURL=forumComment.js.map