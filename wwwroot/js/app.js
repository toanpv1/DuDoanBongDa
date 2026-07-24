// ===== API Configuration =====
const API_BASE = 'https://dudoanbongda-api.onrender.com';
const TOKEN_KEY = 'wcp_token';
const USER_KEY = 'wcp_user';

const Api = {
    getToken() { return localStorage.getItem(TOKEN_KEY); },
    getUser() { try { return JSON.parse(localStorage.getItem(USER_KEY) || '{}'); } catch { return {}; } },
    setAuth(data) {
        localStorage.setItem(TOKEN_KEY, data.token);
        localStorage.setItem(USER_KEY, JSON.stringify({ userId: data.userId, displayName: data.displayName, role: data.role }));
    },
    clearAuth() { localStorage.removeItem(TOKEN_KEY); localStorage.removeItem(USER_KEY); },
    isLoggedIn() { return !!this.getToken(); },
    isAdmin() { const u = this.getUser(); return u.role === 'Admin' || u.role === 'SuperAdmin'; },

    async request(method, url, body) {
        const headers = { 'Content-Type': 'application/json' };
        const token = this.getToken();
        if (token) headers['Authorization'] = `Bearer ${token}`;

        try {
            const res = await fetch(`${API_BASE}${url}`, {
                method, headers,
                body: body ? JSON.stringify(body) : undefined
            });

            if (res.status === 401) {
                this.clearAuth();
                window.location.href = '/login.html';
                return null;
            }

            const data = await res.json().catch(() => null);
            if (!res.ok) throw new Error(data?.message || `Error ${res.status}`);
            return data;
        } catch (err) {
            throw err;
        }
    },

    get(url) { return this.request('GET', url); },
    post(url, body) { return this.request('POST', url, body); },
    put(url, body) { return this.request('PUT', url, body); },
    delete(url) { return this.request('DELETE', url); }
};

// ===== Toast Notifications =====
function showToast(message, type = 'info') {
    let container = document.querySelector('.toast-container');
    if (!container) {
        container = document.createElement('div');
        container.className = 'toast-container';
        document.body.appendChild(container);
    }
    const toast = document.createElement('div');
    toast.className = `toast toast-${type}`;
    const icons = { success: '✅', error: '❌', info: 'ℹ️' };
    toast.innerHTML = `<span>${icons[type] || ''}</span><span>${message}</span>`;
    container.appendChild(toast);
    setTimeout(() => { toast.style.opacity = '0'; setTimeout(() => toast.remove(), 300); }, 3500);
}

// ===== Auth Guard =====
function requireAuth() {
    if (!Api.isLoggedIn()) { window.location.href = '/login.html'; return false; }
    return true;
}

function requireAdmin() {
    if (!requireAuth()) return false;
    if (!Api.isAdmin()) { showToast('Bạn không có quyền truy cập', 'error'); return false; }
    return true;
}

// ===== Sidebar Builder =====
function buildSidebar(activePage) {
    const user = Api.getUser();
    const isAdmin = Api.isAdmin();
    const initial = (user.displayName || 'U').charAt(0).toUpperCase();

    const adminMenu = isAdmin ? `
        <div class="nav-divider"></div>
        <div class="nav-label">Quản trị</div>
        <a href="/admin/tournament.html" class="nav-item ${activePage === 'admin-tournament' ? 'active' : ''}">
            <span class="nav-icon">🏆</span> Cấu hình giải đấu
        </a>
        <a href="/admin/matches.html" class="nav-item ${activePage === 'admin-matches' ? 'active' : ''}">
            <span class="nav-icon">⚙️</span> Quản lý trận đấu
        </a>
        <a href="/admin/results.html" class="nav-item ${activePage === 'admin-results' ? 'active' : ''}">
            <span class="nav-icon">📝</span> Nhập kết quả
        </a>
        <a href="/admin/users.html" class="nav-item ${activePage === 'admin-users' ? 'active' : ''}">
            <span class="nav-icon">👥</span> Quản lý thành viên
        </a>
        <a href="/admin/dashboard.html" class="nav-item ${activePage === 'admin-dashboard' ? 'active' : ''}">
            <span class="nav-icon">📊</span> Thống kê Admin
        </a>
    ` : '';

    return `
    <button class="mobile-toggle" onclick="document.querySelector('.sidebar').classList.toggle('open')">☰</button>
    <aside class="sidebar">
        <div class="sidebar-header">
            <div class="sidebar-logo">⚽ Predictor<span>Dự đoán nội bộ</span></div>
        </div>
        <nav class="sidebar-nav">
            <a href="/index.html" class="nav-item ${activePage === 'dashboard' ? 'active' : ''}">
                <span class="nav-icon">🏠</span> Tổng quan
            </a>
            ${!isAdmin ? `
            <a href="/predictions.html" class="nav-item ${activePage === 'predictions' ? 'active' : ''}">
                <span class="nav-icon">🔮</span> Dự đoán
            </a>
            ` : ''}
            <a href="/leaderboard.html" class="nav-item ${activePage === 'leaderboard' ? 'active' : ''}">
                <span class="nav-icon">🏆</span> Bảng xếp hạng
            </a>
            <a href="/stats.html" class="nav-item ${activePage === 'stats' ? 'active' : ''}">
                <span class="nav-icon">📊</span> Thống kê trận đấu
            </a>
            <a href="/statistics.html" class="nav-item ${activePage === 'statistics' ? 'active' : ''}">
                <span class="nav-icon">📈</span> Thống kê
            </a>
            ${!isAdmin ? `
            <a href="/history.html" class="nav-item ${activePage === 'history' ? 'active' : ''}">
                <span class="nav-icon">📋</span> Lịch sử dự đoán
            </a>
            ` : ''}
            ${adminMenu}
        </nav>
        <div class="sidebar-footer">
            <div class="user-info">
                <div class="user-avatar">${initial}</div>
                <div>
                    <div class="user-name">${user.displayName || 'User'}</div>
                    <div class="user-role">${user.role || ''}</div>
                </div>
            </div>
            <button class="btn btn-outline btn-sm" style="width:100%;margin-top:8px" onclick="showChangePasswordModal()">🔑 Đổi mật khẩu</button>
            <button class="btn btn-outline btn-sm" style="width:100%;margin-top:8px" onclick="Api.clearAuth();location.href='/login.html'">🚪 Đăng xuất</button>
        </div>
    </aside>`;
}

// ===== Utility Functions =====
function formatDate(dateStr) {
    const d = new Date(dateStr);
    return d.toLocaleDateString('vi-VN', { day: '2-digit', month: '2-digit', year: 'numeric' });
}

function formatDateTime(dateStr) {
    const d = new Date(dateStr);
    return d.toLocaleDateString('vi-VN', { day: '2-digit', month: '2-digit' }) + ' ' +
           d.toLocaleTimeString('vi-VN', { hour: '2-digit', minute: '2-digit' });
}

function getActiveTournamentId() {
    return localStorage.getItem('wcp_tournament') || '1';
}

function setActiveTournamentId(id) {
    localStorage.setItem('wcp_tournament', id);
}

// ===== Change Password Modal =====
function showChangePasswordModal() {
    let modal = document.getElementById('cpModal');
    if (!modal) {
        document.body.insertAdjacentHTML('beforeend', `
        <div class="modal-overlay" id="cpModal">
            <div class="modal" style="max-width:400px">
                <div class="modal-header">
                    <h3 class="modal-title">🔑 Đổi mật khẩu</h3>
                    <button class="modal-close" onclick="document.getElementById('cpModal').classList.remove('active')">×</button>
                </div>
                <form id="cpForm">
                    <div style="padding: 20px;">
                        <div class="form-group">
                            <label class="form-label">Mật khẩu hiện tại</label>
                            <input type="password" class="form-input" id="cp_current" required>
                        </div>
                        <div class="form-group">
                            <label class="form-label">Mật khẩu mới</label>
                            <input type="password" class="form-input" id="cp_new" required minlength="6">
                        </div>
                        <div class="form-group">
                            <label class="form-label">Xác nhận mật khẩu mới</label>
                            <input type="password" class="form-input" id="cp_confirm" required minlength="6">
                        </div>
                        <div style="display:flex;gap:8px;justify-content:flex-end;margin-top:16px;">
                            <button type="button" class="btn btn-outline" onclick="document.getElementById('cpModal').classList.remove('active')">Hủy</button>
                            <button type="submit" class="btn btn-primary">Lưu thay đổi</button>
                        </div>
                    </div>
                </form>
            </div>
        </div>
        `);
        modal = document.getElementById('cpModal');
        document.getElementById('cpForm').addEventListener('submit', async e => {
            e.preventDefault();
            const cp = document.getElementById('cp_current').value;
            const np = document.getElementById('cp_new').value;
            const cnp = document.getElementById('cp_confirm').value;
            if (np !== cnp) {
                showToast('Mật khẩu xác nhận không khớp', 'error');
                return;
            }
            try {
                await Api.post('/api/Auth/change-password', { currentPassword: cp, newPassword: np });
                showToast('Đổi mật khẩu thành công', 'success');
                modal.classList.remove('active');
                document.getElementById('cpForm').reset();
            } catch (err) {
                showToast(err.message, 'error');
            }
        });
    }
    modal.classList.add('active');
}
