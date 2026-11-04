// 오류 페이지 공통 스크립트: 버튼 동작 (페이지 문구는 어느 언어권에서나 쓸 수 있게 영어로만 작성)
(function () {
    document.querySelectorAll('[data-act]').forEach(function (el) {
        var act = el.getAttribute('data-act');
        if (act === 'back') {
            // 이전 기록이 없으면(새 탭 등) 홈으로
            el.addEventListener('click', function () { if (history.length > 1) history.back(); else location.href = '/'; });
        } else if (act === 'reload') {
            el.addEventListener('click', function () { location.reload(); });
        } else if (act === 'login') {
            // 오류가 난 주소로 로그인 후 돌아오도록 returnUrl 을 붙인다
            el.setAttribute('href', '/Account/SignIn?returnUrl=' + encodeURIComponent(location.pathname + location.search));
        }
    });
})();
