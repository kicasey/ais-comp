// TalentStrategyAI – Landing → Employee | Manager. EY Talent Match.

var AUTH_STORAGE_KEY = 'talentStrategyAuth';

document.addEventListener('DOMContentLoaded', function () {
    initializeApp();
});

var THEME_STORAGE_KEY = 'talentStrategyTheme';

function initializeApp() {
    applyStoredTheme();
    ensureAuthGate();
    setupLanding();
    setupBackButton();
    setupAuth();
    setupResumeUploadForm();
    loadEmployeeProfileWhenNeeded();
    setupThemeToggle();
    setupChatPresets('employee');
    setupChatPresets('manager');
    setupChatSendButtons();
    setupManagerFlow();
    updateHeaderAuth();
}

function ensureAuthGate() {
    var pageApp = document.getElementById('page-app');
    var pageLanding = document.getElementById('page-landing');
    var pageLogin = document.getElementById('page-login');
    if (!pageApp || !pageLanding) return;
    var auth = getStoredAuth();
    var appVisible = !pageApp.classList.contains('page--hidden');
    if (appVisible && !auth) {
        pageApp.classList.add('page--hidden');
        pageLanding.classList.remove('page--hidden');
        if (pageLogin) pageLogin.classList.add('page--hidden');
        document.body.classList.remove('page--app');
        document.body.classList.remove('page--login');
        document.body.classList.add('page--landing');
    }
}

function getStoredAuth() {
    try {
        var raw = localStorage.getItem(AUTH_STORAGE_KEY);
        if (!raw) return null;
        return JSON.parse(raw);
    } catch (e) {
        return null;
    }
}

function setStoredAuth(data) {
    if (data) localStorage.setItem(AUTH_STORAGE_KEY, JSON.stringify(data));
    else localStorage.removeItem(AUTH_STORAGE_KEY);
    updateHeaderAuth();
    if (!data) goToLanding();
}

function getAuthHeader() {
    var auth = getStoredAuth();
    if (!auth || !auth.token) return {};
    return { 'Authorization': 'Bearer ' + auth.token };
}

function updateHeaderAuth() {
    var auth = getStoredAuth();
    var loginBtn = document.getElementById('btn-login-app');
    var logoutBtn = document.getElementById('btn-logout');
    var userSpan = document.getElementById('header-user');
    if (auth && auth.displayName) {
        if (userSpan) { userSpan.textContent = auth.displayName; userSpan.style.display = 'inline'; }
        if (loginBtn) loginBtn.style.display = 'none';
        if (logoutBtn) logoutBtn.style.display = 'inline-block';
    } else {
        if (userSpan) userSpan.style.display = 'none';
        if (loginBtn) loginBtn.style.display = 'inline-block';
        if (logoutBtn) logoutBtn.style.display = 'none';
    }
}

// ----- Landing: choose Employee or Manager → go to login page -----
var pendingLoginRole = null;

function setupLanding() {
    var pageLanding = document.getElementById('page-landing');
    var pageLogin = document.getElementById('page-login');
    var pageApp = document.getElementById('page-app');
    var goEmployee = document.getElementById('go-employee');
    var goManager = document.getElementById('go-manager');

    if (!pageLanding || !pageLogin) return;

    function goToLoginPage(role) {
        pendingLoginRole = role;
        pageLanding.classList.add('page--hidden');
        if (pageApp) pageApp.classList.add('page--hidden');
        pageLogin.classList.remove('page--hidden');
        document.body.classList.remove('page--landing');
        document.body.classList.remove('page--app');
        document.body.classList.add('page--login');
        var title = document.getElementById('login-page-title');
        var subtitle = document.getElementById('login-page-subtitle');
        if (title) title.textContent = role === 'manager' ? 'Manager sign in' : 'Employee sign in';
        if (subtitle) subtitle.textContent = role === 'manager' ? 'Sign in to view jobs and recommendations.' : 'Sign in to upload your resume and see your matches.';
        var registerRoleSelect = document.getElementById('register-role');
        if (registerRoleSelect) registerRoleSelect.value = role;
        showLoginFormOnPage();
    }

    if (goEmployee) goEmployee.addEventListener('click', function (e) { e.preventDefault(); goToLoginPage('employee'); });
    if (goManager) goManager.addEventListener('click', function (e) { e.preventDefault(); goToLoginPage('manager'); });
}

// ----- Back: from app to landing; from login page to landing -----
function setupBackButton() {
    var btnBack = document.getElementById('btn-back');
    var loginBack = document.getElementById('login-back');
    var pageLanding = document.getElementById('page-landing');
    var pageLogin = document.getElementById('page-login');
    var pageApp = document.getElementById('page-app');
    if (!pageLanding) return;

    if (btnBack) btnBack.addEventListener('click', function () { goToLanding(); });
    if (loginBack) loginBack.addEventListener('click', function (e) { e.preventDefault(); goToLanding(); });
}

function goToLanding() {
    var pageLanding = document.getElementById('page-landing');
    var pageLogin = document.getElementById('page-login');
    var pageApp = document.getElementById('page-app');
    if (pageLanding) pageLanding.classList.remove('page--hidden');
    if (pageLogin) pageLogin.classList.add('page--hidden');
    if (pageApp) pageApp.classList.add('page--hidden');
    document.body.classList.remove('page--app');
    document.body.classList.remove('page--login');
    document.body.classList.add('page--landing');
}

// ----- Auth: login page (full-page) + register (create profile) -----
function showLoginFormOnPage() {
    var loginForm = document.getElementById('auth-login-form');
    var registerForm = document.getElementById('auth-register-form');
    var loginError = document.getElementById('auth-login-error');
    var registerError = document.getElementById('auth-register-error');
    if (loginForm) loginForm.classList.remove('auth-form--hidden');
    if (registerForm) registerForm.classList.add('auth-form--hidden');
    if (loginError) loginError.textContent = '';
    if (registerError) registerError.textContent = '';
}

function setupAuth() {
    var loginForm = document.getElementById('auth-login-form');
    var registerForm = document.getElementById('auth-register-form');
    var showRegister = document.getElementById('auth-show-register');
    var showLogin = document.getElementById('auth-show-login');
    var registerRole = document.getElementById('register-role');
    var employeeFields = document.getElementById('register-employee-fields');
    var loginError = document.getElementById('auth-login-error');
    var registerError = document.getElementById('auth-register-error');
    var pageLogin = document.getElementById('page-login');

    function showRegisterForm() {
        if (loginForm) loginForm.classList.add('auth-form--hidden');
        if (registerForm) registerForm.classList.remove('auth-form--hidden');
        if (loginError) loginError.textContent = '';
        if (registerError) registerError.textContent = '';
        if (pendingLoginRole && registerRole) registerRole.value = pendingLoginRole;
        toggleEmployeeFields();
    }
    function toggleEmployeeFields() {
        if (!employeeFields || !registerRole) return;
        employeeFields.style.display = (registerRole.value || 'employee') === 'employee' ? 'block' : 'none';
    }

    if (showRegister) showRegister.addEventListener('click', showRegisterForm);
    if (showLogin) showLogin.addEventListener('click', showLoginFormOnPage);
    if (registerRole) registerRole.addEventListener('change', toggleEmployeeFields);

    function onLoginSuccess(result) {
        var data = result.data;
        var auth = {
            token: data.token || data.Token,
            userId: data.userId != null ? data.userId : data.UserId,
            email: data.email || data.Email || '',
            displayName: (data.displayName || data.DisplayName || data.email || data.Email || '').toString(),
            role: (data.role || data.Role || 'employee').toLowerCase()
        };
        if (!auth.token) return;
        setStoredAuth(auth);
        if (pageLogin) pageLogin.classList.add('page--hidden');
        document.body.classList.remove('page--login');
        document.body.classList.add('page--app');
        var pageApp = document.getElementById('page-app');
        if (pageApp) pageApp.classList.remove('page--hidden');
        updateHeaderAuth();
        showAppByRole(auth.role);
        if (auth.role === 'manager' && typeof window.loadManagerJobs === 'function') {
            window.loadManagerJobs();
        }
    }

    if (loginForm) {
        loginForm.addEventListener('submit', function (e) {
            e.preventDefault();
            var email = document.getElementById('login-email').value.trim();
            var password = document.getElementById('login-password').value;
            if (loginError) loginError.textContent = '';
            fetch('/api/auth/login', {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({ email: email, password: password })
            })
                .then(function (res) {
                    return res.text().then(function (text) {
                        var data = null;
                        try {
                            data = text ? JSON.parse(text) : null;
                        } catch (err) {
                            data = null;
                        }
                        return { ok: res.ok, status: res.status, data: data, text: text };
                    });
                })
                .then(function (result) {
                    if (result.ok && result.data && (result.data.token || result.data.Token)) {
                        loginForm.reset();
                        onLoginSuccess({ data: result.data });
                    } else {
                        if (loginError) {
                            var msg = (result.data && (result.data.message || result.data.Message)) || (result.status === 401 ? 'Invalid email or password.' : result.status >= 500 ? 'Server error. Make sure the API is running and the database is set up.' : 'Sign in failed. Please try again.');
                            loginError.textContent = msg;
                        }
                    }
                })
                .catch(function (err) {
                    if (loginError) {
                        loginError.textContent = 'Cannot reach the server. Start the API with "dotnet run" and try again.';
                    }
                });
        });
    }

    if (registerForm) {
        registerForm.addEventListener('submit', function (e) {
            e.preventDefault();
            var payload = {
                email: document.getElementById('register-email').value.trim(),
                password: document.getElementById('register-password').value,
                displayName: document.getElementById('register-display-name').value.trim() || undefined,
                role: (pendingLoginRole || document.getElementById('register-role').value) || 'employee',
                position: document.getElementById('register-position').value.trim() || undefined,
                department: document.getElementById('register-department').value.trim() || undefined
            };
            if (registerError) registerError.textContent = '';
            fetch('/api/auth/register', {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify(payload)
            })
                .then(function (res) {
                    return res.text().then(function (text) {
                        var data = null;
                        try { data = text ? JSON.parse(text) : null; } catch (e) { data = null; }
                        return { ok: res.ok, status: res.status, data: data };
                    });
                })
                .then(function (result) {
                    if (result.ok && result.data && (result.data.token || result.data.Token)) {
                        registerForm.reset();
                        onLoginSuccess({ data: result.data });
                    } else {
                        if (registerError) {
                            registerError.textContent = (result.data && (result.data.message || result.data.Message)) || (result.status >= 500 ? 'Server error. Check that the API and database are running.' : 'Registration failed.');
                        }
                    }
                })
                .catch(function () {
                    if (registerError) registerError.textContent = 'Cannot reach the server. Start the API with "dotnet run" and try again.';
                });
        });
    }

    var logoutBtn = document.getElementById('btn-logout');
    if (logoutBtn) logoutBtn.addEventListener('click', function () { setStoredAuth(null); });

    var btnLoginApp = document.getElementById('btn-login-app');
    if (btnLoginApp) btnLoginApp.addEventListener('click', function () {
        var pageLanding = document.getElementById('page-landing');
        var pageApp = document.getElementById('page-app');
        var pageLogin = document.getElementById('page-login');
        if (pageApp) pageApp.classList.add('page--hidden');
        if (pageLogin) pageLogin.classList.add('page--hidden');
        if (pageLanding) pageLanding.classList.remove('page--hidden');
        document.body.classList.remove('page--app');
        document.body.classList.remove('page--login');
        document.body.classList.add('page--landing');
    });
}

function showAppByRole(role) {
    if (!getStoredAuth()) return;
    var pageLanding = document.getElementById('page-landing');
    var pageApp = document.getElementById('page-app');
    if (!pageLanding || !pageApp) return;
    pageLanding.classList.add('page--hidden');
    pageApp.classList.remove('page--hidden');
    document.body.classList.remove('page--landing');
    document.body.classList.add('page--app');
    var emp = document.getElementById('interface-employee');
    var mgr = document.getElementById('interface-manager');
    if (role === 'manager') {
        if (emp) emp.classList.add('interface--hidden');
        if (mgr) mgr.classList.remove('interface--hidden');
        resetManagerPanel();
        if (typeof window.loadManagerJobs === 'function') window.loadManagerJobs();
    } else {
        if (emp) emp.classList.remove('interface--hidden');
        if (mgr) mgr.classList.add('interface--hidden');
        loadEmployeeProfile();
    }
}

function loadEmployeeProfileWhenNeeded() {
    var auth = getStoredAuth();
    if (!auth || (auth.role || '').toLowerCase() !== 'employee') return;
    var emp = document.getElementById('interface-employee');
    if (emp && !emp.classList.contains('interface--hidden')) loadEmployeeProfile();
}

function loadEmployeeProfile() {
    var nameEl = document.getElementById('profile-name');
    var emailEl = document.getElementById('profile-email');
    var valueEl = document.getElementById('profile-resume-value');
    if (!valueEl) return;
    var headers = getAuthHeader();
    if (!headers.Authorization) {
        if (nameEl) nameEl.textContent = '';
        if (emailEl) emailEl.textContent = '';
        valueEl.textContent = 'Sign in to see your profile.';
        return;
    }
    valueEl.textContent = 'Loading…';
    fetch('/api/resume/profile', { headers: headers })
        .then(function (res) { return res.json().catch(function () { return null; }); })
        .then(function (data) {
            if (!data) { valueEl.textContent = 'Could not load profile.'; return; }
            if (data.isEmployee) {
                if (nameEl) nameEl.textContent = (data.displayName || data.name || '').trim() || '—';
                if (emailEl) emailEl.textContent = (data.email || '').trim() || '—';
                if (data.hasResume && data.resumeFileName) {
                    valueEl.textContent = data.resumeFileName + (data.resumeUploadedAt ? ' (uploaded ' + formatResumeDate(data.resumeUploadedAt) + ')' : '');
                    valueEl.classList.add('has-resume');
                } else {
                    valueEl.textContent = 'No resume on file. Upload one below.';
                    valueEl.classList.remove('has-resume');
                }
            } else {
                if (nameEl) nameEl.textContent = '';
                if (emailEl) emailEl.textContent = '';
                valueEl.textContent = 'Resume is for employee profiles only.';
                valueEl.classList.remove('has-resume');
            }
        })
        .catch(function () { valueEl.textContent = 'Could not load profile.'; });
}

function formatResumeDate(iso) {
    if (!iso) return '';
    try {
        var d = new Date(iso);
        if (isNaN(d.getTime())) return '';
        return d.toLocaleDateString(undefined, { month: 'short', day: 'numeric', year: 'numeric' });
    } catch (e) { return ''; }
}

function applyStoredTheme() {
    var theme = localStorage.getItem(THEME_STORAGE_KEY) || 'dark';
    document.body.setAttribute('data-theme', theme);
}

function setupThemeToggle() {
    var toggles = document.querySelectorAll('.theme-toggle');
    function updateAccessibility() {
        var theme = document.body.getAttribute('data-theme') || 'dark';
        var title = theme === 'dark' ? 'Switch to light mode' : 'Switch to dark mode';
        var isDark = theme === 'dark';
        toggles.forEach(function (el) {
            el.setAttribute('title', title);
            el.setAttribute('aria-checked', isDark ? 'true' : 'false');
        });
    }
    updateAccessibility();
    toggles.forEach(function (el) {
        el.addEventListener('click', function () {
            var theme = document.body.getAttribute('data-theme') || 'dark';
            var next = theme === 'dark' ? 'light' : 'dark';
            document.body.setAttribute('data-theme', next);
            localStorage.setItem(THEME_STORAGE_KEY, next);
            updateAccessibility();
        });
    });
}

function resetManagerPanel() {
    var panel = document.getElementById('manager-job-panel');
    var wrap = document.getElementById('manager-recommendations-wrap');
    if (panel) {
        panel.classList.add('manager-panel--closed');
        panel.classList.remove('manager-panel--open');
    }
    if (wrap) wrap.style.display = 'none';
}

// ----- Resume upload: file input only covers the label (no whole-page click) -----
function setupResumeUploadForm() {
    var form = document.getElementById('resume-upload-form');
    var fileInput = document.getElementById('resume-file');
    var fileLabel = document.getElementById('resume-file-label');
    var submitBtn = document.getElementById('submit-btn');
    var btnText = submitBtn ? submitBtn.querySelector('.btn-text') : null;
    var btnLoader = submitBtn ? submitBtn.querySelector('.btn-loader') : null;

    if (!fileInput || !fileLabel || !form) return;

    fileInput.addEventListener('change', function (e) {
        var file = e.target.files[0];
        if (file) {
            fileLabel.textContent = file.name;
            fileLabel.classList.add('file-selected');
            var maxSize = 10 * 1024 * 1024;
            if (file.size > maxSize) {
                showUploadMessage('File size exceeds 10MB. Choose a smaller file.', 'error');
                fileInput.value = '';
                fileLabel.textContent = 'Choose file...';
                fileLabel.classList.remove('file-selected');
                return;
            }
            var allowedTypes = ['application/pdf', 'application/msword', 'application/vnd.openxmlformats-officedocument.wordprocessingml.document'];
            if (allowedTypes.indexOf(file.type) === -1) {
                showUploadMessage('Please upload a PDF, DOC, or DOCX file.', 'error');
                fileInput.value = '';
                fileLabel.textContent = 'Choose file...';
                fileLabel.classList.remove('file-selected');
                return;
            }
            hideUploadMessage();
        } else {
            fileLabel.textContent = 'Choose file...';
            fileLabel.classList.remove('file-selected');
        }
    });

    form.addEventListener('submit', async function (e) {
        e.preventDefault();
        var file = fileInput.files[0];
        if (!file) {
            showUploadMessage('Please select a resume file.', 'error');
            return;
        }
        if (btnText) btnText.style.display = 'none';
        if (btnLoader) btnLoader.style.display = 'inline';
        submitBtn.disabled = true;
        hideUploadMessage();

        try {
            var formData = new FormData();
            formData.append('resume', file);
            var headers = getAuthHeader();
            var response = await fetch('/api/resume/upload', { method: 'POST', headers: headers, body: formData });
            var result = await response.json().catch(function () { return {}; });
            if (response.ok) {
                showUploadMessage('Resume saved to your profile.', 'success');
                form.reset();
                fileLabel.textContent = 'Choose file...';
                fileLabel.classList.remove('file-selected');
                loadEmployeeProfile();
            } else {
                showUploadMessage(result.message || 'Upload failed. Please try again.', 'error');
            }
        } catch (err) {
            console.error('Upload error:', err);
            showUploadMessage('An error occurred. Please try again.', 'error');
        } finally {
            submitBtn.disabled = false;
            if (btnText) btnText.style.display = 'inline';
            if (btnLoader) btnLoader.style.display = 'none';
        }
    });
}

function showUploadMessage(message, type) {
    var el = document.getElementById('upload-message');
    if (!el) return;
    el.textContent = message;
    el.className = 'message message-' + type;
    el.style.display = 'block';
}

function hideUploadMessage() {
    var el = document.getElementById('upload-message');
    if (el) el.style.display = 'none';
}

// ----- Chat: preset buttons -----
function setupChatPresets(role) {
    var container = role === 'employee' ? document.getElementById('interface-employee') : document.getElementById('interface-manager');
    if (!container) return;
    var presetButtons = container.querySelectorAll('.preset-btn');
    var messagesEl = role === 'employee' ? document.getElementById('chat-messages-employee') : document.getElementById('chat-messages-manager');
    if (!messagesEl) return;

    presetButtons.forEach(function (btn) {
        btn.addEventListener('click', function (e) {
            e.preventDefault();
            var preset = this.getAttribute('data-preset');
            sendPresetAndShowResponse(preset, messagesEl);
        });
    });
}

// ----- Chat: send button + text input -----
function setupChatSendButtons() {
    var sendEmployee = document.getElementById('chat-send-employee');
    var sendManager = document.getElementById('chat-send-manager');
    var inputEmployee = document.getElementById('chat-input-employee');
    var inputManager = document.getElementById('chat-input-manager');
    var messagesEmp = document.getElementById('chat-messages-employee');
    var messagesMgr = document.getElementById('chat-messages-manager');

    function sendFromInput(inputEl, messagesEl) {
        if (!inputEl || !messagesEl) return;
        var text = (inputEl.value || '').trim();
        if (!text) return;
        inputEl.value = '';
        appendChatMessage(messagesEl, 'You', text, 'user');
        var loadingId = 'loading-' + Date.now();
        appendChatMessage(messagesEl, 'Assistant', '…', 'assistant', true, loadingId);
        var headers = Object.assign({ 'Content-Type': 'application/json' }, getAuthHeader());
        fetch('/api/chat', {
            method: 'POST',
            headers: headers,
            body: JSON.stringify({ preset: 'custom', customText: text })
        })
            .then(function (res) {
                removeLoadingMessage(messagesEl, loadingId);
                if (res.ok) return res.json();
                return res.json().then(function (data) { throw new Error(data.message || 'Request failed'); });
            })
            .then(function (data) {
                var responseText = (data && data.response) ? data.response : (data && data.message) ? data.message : 'Done.';
                appendChatMessage(messagesEl, 'Assistant', responseText, 'assistant');
            })
            .catch(function (err) {
                removeLoadingMessage(messagesEl, loadingId);
                var msg = (err && err.message) ? err.message : 'The assistant could not respond. Connect the backend to resume-api for AI.';
                appendChatMessage(messagesEl, 'Assistant', msg, 'assistant');
            });
    }

    if (sendEmployee && inputEmployee && messagesEmp) {
        sendEmployee.addEventListener('click', function () { sendFromInput(inputEmployee, messagesEmp); });
        inputEmployee.addEventListener('keydown', function (e) {
            if (e.key === 'Enter' && !e.shiftKey) {
                e.preventDefault();
                sendFromInput(inputEmployee, messagesEmp);
            }
        });
    }
    if (sendManager && inputManager && messagesMgr) {
        sendManager.addEventListener('click', function () { sendFromInput(inputManager, messagesMgr); });
        inputManager.addEventListener('keydown', function (e) {
            if (e.key === 'Enter' && !e.shiftKey) {
                e.preventDefault();
                sendFromInput(inputManager, messagesMgr);
            }
        });
    }
}

function sendPresetAndShowResponse(preset, messagesEl) {
    appendChatMessage(messagesEl, 'You', getPresetLabel(preset), 'user');
    var loadingId = 'loading-' + Date.now();
    appendChatMessage(messagesEl, 'Assistant', '…', 'assistant', true, loadingId);

    var headers = Object.assign({ 'Content-Type': 'application/json' }, getAuthHeader());
    fetch('/api/chat', {
        method: 'POST',
        headers: headers,
        body: JSON.stringify({ preset: preset })
    })
        .then(function (res) {
            removeLoadingMessage(messagesEl, loadingId);
            if (res.ok) return res.json();
            return res.json().then(function (data) { throw new Error(data.message || 'Request failed'); });
        })
        .then(function (data) {
            var text = (data && data.response) ? data.response : (data && data.message) ? data.message : (typeof data === 'string') ? data : 'Done.';
            appendChatMessage(messagesEl, 'Assistant', text, 'assistant');
        })
        .catch(function (err) {
            removeLoadingMessage(messagesEl, loadingId);
            var fallback = (err && err.message) ? err.message : 'The assistant could not respond. Check that the backend is connected to resume-api for AI and SQL.';
            appendChatMessage(messagesEl, 'Assistant', fallback, 'assistant');
        });
}

function getPresetLabel(preset) {
    var labels = {
        match_roles: 'Match my resume to open EY roles',
        explain_match: 'Explain why I was matched (or not) to a role',
        suggest_upskill: 'Suggest upskilling to improve my fit',
        match_summary: 'Show my match summary',
        match_employees_to_role: 'Match employees to an open role',
        explain_employee_match: 'Explain why an employee was matched (or not) to a role',
        top_employees_for_role: 'See top employees for a role by match %',
        team_match_summary: 'View match summary for my team'
    };
    return labels[preset] || preset;
}

function appendChatMessage(container, role, text, roleClass, isLoading, id) {
    var div = document.createElement('div');
    div.className = 'chat-message chat-message--' + roleClass + (isLoading ? ' chat-message--loading' : '');
    if (id) div.id = id;
    div.innerHTML = '<div class="chat-message__role">' + escapeHtml(role) + '</div><div class="chat-message__text">' + escapeHtml(text) + '</div>';
    container.appendChild(div);
    container.scrollTop = container.scrollHeight;
}

function removeLoadingMessage(container, id) {
    var el = document.getElementById(id);
    if (el && el.parentNode === container) el.remove();
}

function escapeHtml(s) {
    var div = document.createElement('div');
    div.textContent = s;
    return div.innerHTML;
}

// ----- Manager flow: jobs list → job panel → recommend employees → employee popout (AI explanation) -----
function setupManagerFlow() {
    var jobsListEl = document.getElementById('manager-jobs-list');
    var jobPanel = document.getElementById('manager-job-panel');
    var jobDetailEl = document.getElementById('manager-job-detail');
    var panelClose = document.getElementById('manager-panel-close');
    var btnRecommend = document.getElementById('btn-recommend-employees');
    var recommendationsWrap = document.getElementById('manager-recommendations-wrap');
    var recommendationsListEl = document.getElementById('manager-recommendations-list');
    var recommendationsJobTitle = document.getElementById('manager-recommendations-job-title');
    var employeePopout = document.getElementById('manager-employee-popout');
    var employeePopoutClose = document.getElementById('employee-popout-close');
    var employeePopoutTitle = document.getElementById('employee-popout-title');
    var employeePopoutContent = document.getElementById('employee-popout-content');
    var employeePopoutSingle = document.getElementById('employee-popout-single');
    var employeePopoutCompare = document.getElementById('employee-popout-compare');
    var compareNameA = document.getElementById('employee-popout-compare-name-a');
    var compareContentA = document.getElementById('employee-popout-compare-content-a');
    var compareNameB = document.getElementById('employee-popout-compare-name-b');
    var compareContentB = document.getElementById('employee-popout-compare-content-b');
    var compareBtn = document.getElementById('employee-popout-compare-btn');
    var backSingleBtn = document.getElementById('employee-popout-back-single');
    var compareHint = document.getElementById('employee-popout-compare-hint');
    var popoutResizeHandle = document.getElementById('employee-popout-resize');

    var compareMode = false;
    var firstEmployeeForCompare = null;
    var popoutHeightPx = null;

    var selectedJobId = null;
    var selectedJobTitle = null;

    function openJobPanel(job) {
        var j = job || {};
        selectedJobId = j.id || j.Id;
        selectedJobTitle = j.title || j.Title || '';
        jobDetailEl.innerHTML =
            '<div class="job-detail-title">' + escapeHtml(j.title || j.Title || '') + '</div>' +
            '<div class="job-detail-meta">' + escapeHtml(j.department || j.Department || '') + ' · ' + escapeHtml(j.location || j.Location || '') + '</div>' +
            '<div class="job-detail-desc">' + escapeHtml(j.description || j.Description || '') + '</div>';
        jobPanel.classList.remove('manager-panel--closed');
        jobPanel.classList.add('manager-panel--open');
        recommendationsWrap.style.display = 'none';
    }

    function closeJobPanel() {
        jobPanel.classList.add('manager-panel--closed');
        jobPanel.classList.remove('manager-panel--open');
        selectedJobId = null;
        selectedJobTitle = null;
    }

    if (panelClose) panelClose.addEventListener('click', closeJobPanel);

    function loadManagerJobs() {
        if (!jobsListEl) return;
        jobsListEl.innerHTML = '<p class="placeholder-text">Loading jobs…</p>';
        fetch('/api/jobs', { headers: getAuthHeader() })
            .then(function (res) { return res.ok ? res.json() : Promise.reject(new Error('Failed to load jobs')); })
            .then(function (jobs) {
                jobsListEl.innerHTML = '';
                (jobs || []).forEach(function (job) {
                    var jid = job.id || job.Id;
                    var jtitle = job.title || job.Title || '';
                    var jdept = job.department || job.Department || '';
                    var jloc = job.location || job.Location || '';
                    var card = document.createElement('button');
                    card.type = 'button';
                    card.className = 'job-card';
                    card.setAttribute('data-job-id', jid);
                    card.innerHTML = '<div class="job-card__title">' + escapeHtml(jtitle) + '</div><div class="job-card__meta">' + escapeHtml(jdept) + ' · ' + escapeHtml(jloc) + '</div>';
                    card.addEventListener('click', function () {
                        fetch('/api/jobs/' + encodeURIComponent(jid), { headers: getAuthHeader() })
                            .then(function (r) { return r.ok ? r.json() : Promise.reject(new Error('Job not found')); })
                            .then(function (detail) {
                                var d = detail || {};
                                openJobPanel({
                                    id: d.id || d.Id || jid,
                                    title: d.title || d.Title || jtitle,
                                    department: d.department || d.Department || jdept,
                                    location: d.location || d.Location || jloc,
                                    description: d.description || d.Description || ''
                                });
                            })
                            .catch(function () { jobsListEl.innerHTML = '<p class="placeholder-text">Could not load job details.</p>'; });
                    });
                    jobsListEl.appendChild(card);
                });
            })
            .catch(function () {
                jobsListEl.innerHTML = '<p class="placeholder-text">Could not load jobs. Try again later.</p>';
            });
    }
    window.loadManagerJobs = loadManagerJobs;

    if (btnRecommend) {
        btnRecommend.addEventListener('click', function () {
            if (!selectedJobId) return;
            recommendationsListEl.innerHTML = '<p class="placeholder-text">Loading recommendations…</p>';
            recommendationsWrap.style.display = 'block';
            if (recommendationsJobTitle) recommendationsJobTitle.textContent = ' for \"' + (selectedJobTitle || '') + '\"';
            fetch('/api/jobs/' + encodeURIComponent(selectedJobId) + '/recommendations', { headers: getAuthHeader() })
                .then(function (res) { return res.ok ? res.json() : Promise.reject(new Error('Failed to load recommendations')); })
                .then(function (list) {
                    recommendationsListEl.innerHTML = '';
                    list.forEach(function (emp) {
                        var card = document.createElement('button');
                        card.type = 'button';
                        card.className = 'recommendation-card';
                        card.setAttribute('data-employee-id', emp.employeeId || emp.EmployeeId || '');
                        card.setAttribute('data-employee-name', emp.name || emp.Name || '');
                        var pct = emp.confidencePercent != null ? emp.confidencePercent : (emp.ConfidencePercent != null ? emp.ConfidencePercent : null);
                        card.innerHTML = '<span class="recommendation-card__name">' + escapeHtml(emp.name || emp.Name || '') + '</span><span class="recommendation-card__pct">' + (pct != null ? pct + '%' : '—') + '</span>';
                        card.addEventListener('click', function () {
                            if (compareMode && firstEmployeeForCompare) {
                                addSecondEmployeeForCompare(emp.employeeId || emp.EmployeeId || '', emp.name || emp.Name || 'Employee');
                            } else {
                                openEmployeePopout(emp.employeeId || emp.EmployeeId || '', emp.name || emp.Name || 'Employee');
                            }
                        });
                        recommendationsListEl.appendChild(card);
                    });
                })
                .catch(function () {
                    recommendationsListEl.innerHTML = '<p class="placeholder-text">Could not load recommendations.</p>';
                });
        });
    }

    var popoutDragBtn = document.getElementById('employee-popout-drag');
    function setPopoutCollapsed(collapsed) {
        if (collapsed) {
            employeePopout.classList.add('employee-popout--collapsed');
            if (popoutDragBtn) popoutDragBtn.textContent = '▲';
        } else {
            employeePopout.classList.remove('employee-popout--collapsed');
            if (popoutDragBtn) popoutDragBtn.textContent = '▼';
        }
    }

    function showSingleView() {
        if (employeePopoutSingle) employeePopoutSingle.style.display = '';
        if (employeePopoutCompare) employeePopoutCompare.style.display = 'none';
        if (compareBtn) compareBtn.style.display = '';
        if (backSingleBtn) backSingleBtn.style.display = 'none';
        if (compareHint) compareHint.style.display = 'none';
        compareMode = false;
        firstEmployeeForCompare = null;
    }

    function openEmployeePopout(employeeId, employeeName) {
        showSingleView();
        employeePopoutTitle.textContent = 'Match explanation: ' + employeeName;
        employeePopoutContent.textContent = 'Loading AI explanation…';
        employeePopout.classList.remove('employee-popout--closed');
        employeePopout.classList.add('employee-popout--open');
        setPopoutCollapsed(false);
        applyPopoutHeight();

        var popoutHeaders = Object.assign({ 'Content-Type': 'application/json' }, getAuthHeader());
        fetch('/api/chat', {
            method: 'POST',
            headers: popoutHeaders,
            body: JSON.stringify({
                preset: 'explain_employee_match',
                jobId: selectedJobId,
                employeeId: employeeId
            })
        })
            .then(function (res) { return res.json(); })
            .then(function (data) {
                var text = (data && data.response) ? data.response : (data && data.message) ? data.message : 'No explanation available.';
                employeePopoutContent.textContent = text;
            })
            .catch(function () {
                employeePopoutContent.textContent = 'Could not load explanation. The API may not be connected yet.';
            });
    }

    function addSecondEmployeeForCompare(employeeId, employeeName) {
        if (!firstEmployeeForCompare) return;
        compareContentB.textContent = 'Loading…';
        compareNameA.textContent = firstEmployeeForCompare.name;
        compareContentA.textContent = firstEmployeeForCompare.content;
        compareNameB.textContent = employeeName;
        employeePopoutSingle.style.display = 'none';
        employeePopoutCompare.style.display = 'grid';
        compareBtn.style.display = 'none';
        backSingleBtn.style.display = '';
        compareHint.style.display = 'none';

        var popoutHeaders = Object.assign({ 'Content-Type': 'application/json' }, getAuthHeader());
        fetch('/api/chat', {
            method: 'POST',
            headers: popoutHeaders,
            body: JSON.stringify({
                preset: 'explain_employee_match',
                jobId: selectedJobId,
                employeeId: employeeId
            })
        })
            .then(function (res) { return res.json(); })
            .then(function (data) {
                var text = (data && data.response) ? data.response : (data && data.message) ? data.message : 'No explanation available.';
                compareContentB.textContent = text;
            })
            .catch(function () {
                compareContentB.textContent = 'Could not load explanation.';
            });
        compareMode = false;
        firstEmployeeForCompare = null;
    }

    function startCompareMode() {
        compareMode = true;
        firstEmployeeForCompare = {
            id: null,
            name: employeePopoutTitle.textContent.replace(/^Match explanation:\s*/i, ''),
            content: employeePopoutContent.textContent
        };
        compareHint.style.display = 'block';
        compareHint.textContent = 'Click another employee above to compare.';
    }

    function closeEmployeePopout() {
        employeePopout.classList.add('employee-popout--closed');
        employeePopout.classList.remove('employee-popout--open');
        employeePopout.classList.remove('employee-popout--collapsed');
        if (popoutDragBtn) popoutDragBtn.textContent = '▼';
        showSingleView();
        if (employeePopout.style.height) employeePopout.style.height = '';
    }

    function applyPopoutHeight() {
        if (!employeePopout.classList.contains('employee-popout--open') || employeePopout.classList.contains('employee-popout--collapsed')) return;
        if (popoutHeightPx != null) {
            employeePopout.style.maxHeight = '';
            employeePopout.style.height = popoutHeightPx + 'px';
        } else {
            employeePopout.style.height = '';
            employeePopout.style.maxHeight = '50vh';
        }
    }

    function setupPopoutResize() {
        if (!popoutResizeHandle) return;
        var minH = 120;
        var maxH = Math.max(200, window.innerHeight * 0.9);

        popoutResizeHandle.addEventListener('mousedown', function (e) {
            if (!employeePopout.classList.contains('employee-popout--open') || employeePopout.classList.contains('employee-popout--collapsed')) return;
            e.preventDefault();
            var startY = e.clientY;
            var startHeight = popoutHeightPx != null ? popoutHeightPx : Math.min(400, window.innerHeight * 0.5);
            if (employeePopout.style.height) startHeight = parseInt(employeePopout.style.height, 10) || startHeight;
            employeePopout.classList.add('employee-popout--resizing');

            function onMove(e) {
                var dy = startY - e.clientY;
                var newH = Math.min(maxH, Math.max(minH, startHeight + dy));
                popoutHeightPx = newH;
                employeePopout.style.maxHeight = '';
                employeePopout.style.height = newH + 'px';
            }
            function onUp() {
                document.removeEventListener('mousemove', onMove);
                document.removeEventListener('mouseup', onUp);
                employeePopout.classList.remove('employee-popout--resizing');
            }
            document.addEventListener('mousemove', onMove);
            document.addEventListener('mouseup', onUp);
        });
    }

    if (compareBtn) compareBtn.addEventListener('click', startCompareMode);
    if (backSingleBtn) backSingleBtn.addEventListener('click', function () {
        var name = compareNameA ? compareNameA.textContent : '';
        var content = compareContentA ? compareContentA.textContent : '';
        showSingleView();
        employeePopoutTitle.textContent = 'Match explanation: ' + name;
        employeePopoutContent.textContent = content;
    });
    setupPopoutResize();

    if (employeePopoutClose) employeePopoutClose.addEventListener('click', closeEmployeePopout);
    if (popoutDragBtn) {
        popoutDragBtn.addEventListener('click', function () {
            if (!employeePopout.classList.contains('employee-popout--open')) return;
            var collapsed = employeePopout.classList.toggle('employee-popout--collapsed');
            popoutDragBtn.textContent = collapsed ? '▲' : '▼';
            if (!collapsed) applyPopoutHeight();
        });
    }
}
