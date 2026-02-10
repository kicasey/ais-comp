// TalentStrategyAI – Landing → Employee | Manager. EY Talent Match.

document.addEventListener('DOMContentLoaded', function () {
    initializeApp();
});

function initializeApp() {
    setupLanding();
    setupBackButton();
    setupLoginPlaceholder();
    setupResumeUploadForm();
    setupChatPresets('employee');
    setupChatPresets('manager');
    setupChatSendButtons();
}

// ----- Landing: choose Employee or Manager -----
function setupLanding() {
    var pageLanding = document.getElementById('page-landing');
    var pageApp = document.getElementById('page-app');
    var goEmployee = document.getElementById('go-employee');
    var goManager = document.getElementById('go-manager');

    if (!pageLanding || !pageApp) return;

    function showApp(role) {
        pageLanding.classList.add('page--hidden');
        pageApp.classList.remove('page--hidden');
        document.body.classList.remove('page--landing');
        document.body.classList.add('page--app');
        var emp = document.getElementById('interface-employee');
        var mgr = document.getElementById('interface-manager');
        if (role === 'employee') {
            if (emp) emp.classList.remove('interface--hidden');
            if (mgr) mgr.classList.add('interface--hidden');
        } else {
            if (emp) emp.classList.add('interface--hidden');
            if (mgr) mgr.classList.remove('interface--hidden');
        }
    }

    if (goEmployee) goEmployee.addEventListener('click', function () { showApp('employee'); });
    if (goManager) goManager.addEventListener('click', function () { showApp('manager'); });
}

// ----- Back to landing -----
function setupBackButton() {
    var btnBack = document.getElementById('btn-back');
    var pageLanding = document.getElementById('page-landing');
    var pageApp = document.getElementById('page-app');
    if (!btnBack || !pageLanding || !pageApp) return;
    btnBack.addEventListener('click', function () {
        pageApp.classList.add('page--hidden');
        pageLanding.classList.remove('page--hidden');
        document.body.classList.remove('page--app');
        document.body.classList.add('page--landing');
    });
}

// ----- Login placeholder -----
function setupLoginPlaceholder() {
    var btns = document.querySelectorAll('#btn-login, #btn-login-app');
    btns.forEach(function (btn) {
        if (!btn) return;
        btn.addEventListener('click', function () {
            alert('Login will connect to EY SSO. This is a placeholder.');
        });
    });
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
            var formData = new FormData(form);
            var response = await fetch('/api/resume/upload', { method: 'POST', body: formData });
            var result = await response.json();
            if (response.ok) {
                showUploadMessage('Resume uploaded successfully!', 'success');
                form.reset();
                fileLabel.textContent = 'Choose file...';
                fileLabel.classList.remove('file-selected');
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
        fetch('/api/chat', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
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
            .catch(function () {
                removeLoadingMessage(messagesEl, loadingId);
                appendChatMessage(messagesEl, 'Assistant', 'The assistant is not connected yet. Connect the backend to resume-api for AI responses.', 'assistant');
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

    fetch('/api/chat', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ preset: preset })
    })
        .then(function (res) {
            removeLoadingMessage(messagesEl, loadingId);
            if (res.ok) return res.json();
            return res.json().then(function (data) { throw new Error(data.message || 'Request failed'); });
        })
        .then(function (data) {
            var text = (data && data.response) ? data.response : (data && data.message) ? data.message : 'Done.';
            appendChatMessage(messagesEl, 'Assistant', text, 'assistant');
        })
        .catch(function () {
            removeLoadingMessage(messagesEl, loadingId);
            appendChatMessage(messagesEl, 'Assistant', 'The assistant is not connected yet. Connect the backend to resume-api for AI and SQL.', 'assistant');
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
