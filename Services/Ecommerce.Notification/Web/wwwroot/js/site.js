// site.js — single-file loader, chắc chắn chạy
(async () => {
    const log = (...a) => console.log("%c[SignalR]", "color:#06c", ...a);
    const warn = (...a) => console.warn("%c[SignalR]", "color:#c60", ...a);
    const error = (...a) => console.error("%c[SignalR]", "color:#c00", ...a);

    // 1) Nạp SignalR từ CDN và ĐỢI load xong
    const CDN = "https://cdn.jsdelivr.net/npm/@microsoft/signalr@8/dist/browser/signalr.min.js";
    async function ensureSignalR() {
        if (window.signalR?.HubConnectionBuilder) return window.signalR;
        log("Loading SignalR from", CDN);
        await new Promise((resolve, reject) => {
            const s = document.createElement("script");
            s.src = CDN;
            s.async = true;
            s.onload = resolve;
            s.onerror = () => reject(new Error("Failed to load " + CDN));
            document.head.appendChild(s);
        });
        return window.signalR;
    }

    let SR;
    try {
        SR = await ensureSignalR();
    } catch (e) {
        error(e.message);
        return;
    }
    if (!SR?.HubConnectionBuilder) {
        error("SignalR not available after load");
        return;
    }
    log("SignalR ready");

    // 2) Phần kết nối hub của bạn (giữ nguyên, đổi signalR -> SR)
    const baseUrl = "https://localhost:7180";
    const hubUrl = `${baseUrl}/hubs/notifications`;

    function getToken() { return localStorage.getItem("access_token") || ""; }
    const tokenPreview = t => (t ? `${t.slice(0, 12)}… (${t.length} chars)` : "(empty)");

    const connection = new SR.HubConnectionBuilder()
        .configureLogging(SR.LogLevel.Trace)
        .withUrl(hubUrl, {
            accessTokenFactory: () => {
                const t = getToken();
                log("accessTokenFactory ->", tokenPreview(t));
                return t;
            },
            withCredentials: true,
        })
        .withAutomaticReconnect([0, 2000, 5000, 10000])
        .build();

    connection.serverTimeoutInMilliseconds = 30_000;
    connection.keepAliveIntervalInMilliseconds = 15_000;

    connection.on("notification:created", payload => {
        log("EVENT notification:created", payload);
        // tăng badge hiện tại
        const badge = document.getElementById("notif-badge");
        const cur = parseInt(badge?.innerText || "0", 10) || 0;
        showBadge(cur + 1);
        // thêm vào danh sách nếu đang ở trang Notifications
        addToList(payload);
    });
    // --- đặt dưới chỗ khai báo connection, trước start() ---

    function showBadge(n) {
        const badge = document.getElementById("notif-badge");
        if (!badge) return;
        badge.innerText = String(n);
        badge.style.display = n > 0 ? "inline-block" : "none";
    }

    function addToList(item) {
        const list = document.getElementById("notif-list");
        if (!list) return; // đang ở trang khác
        const li = document.createElement("li");
        const isRead = !!item.isRead; // server gửi false cho cái mới
        const created = item.createdAt
            ? new Date(item.createdAt).toLocaleString()
            : "";
        li.style.marginBottom = "8px";
        if (!isRead) li.style.fontWeight = "bold";
        li.innerHTML =
            `<div>
        <strong>${item.title}</strong> — ${item.message}
        <small>(${created})</small>
        ${item.href ? `<a href="${item.href}" target="_blank">link</a>` : ""}
     </div>
     ${!isRead ? `
       <form method="post" action="?handler=MarkRead">
         <input type="hidden" name="id" value="${item.id}" />
         <button type="submit">Mark as read</button>
       </form>` : ""}`;
        // mới nhất lên đầu
        list.prepend(li);
    }

    // Cho Razor page dùng để set badge ban đầu
    window.__notif = {
        showBadge,
        addToList,
    };


    async function start() {
        try {
            log("START connecting…", { hubUrl, state: connection.state });
            await connection.start();
            log("CONNECTED", { connectionId: connection.connectionId, state: connection.state });
        } catch (e) {
            error("START failed", e?.message || e);
            setTimeout(start, 2000);
        }
    }

    start();
})();
