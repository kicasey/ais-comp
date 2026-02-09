// TalentStrategyAI Frontend Application
// This file will contain the frontend JavaScript logic

document.addEventListener('DOMContentLoaded', function() {
    console.log('TalentStrategyAI Frontend Application Loaded');
    
    // Frontend application code will be added here
    initializeApp();
});

function initializeApp() {
    // Initialize the application
    console.log('Application initialized');
    
    // Setup resume upload form
    setupResumeUploadForm();
}

function setupResumeUploadForm() {
    const form = document.getElementById('resume-upload-form');
    const fileInput = document.getElementById('resume-file');
    const fileLabel = document.querySelector('.file-input-label');
    const submitBtn = document.getElementById('submit-btn');
    const btnText = submitBtn.querySelector('.btn-text');
    const btnLoader = submitBtn.querySelector('.btn-loader');
    const messageDiv = document.getElementById('upload-message');
    
    // Update file label when file is selected
    fileInput.addEventListener('change', function(e) {
        const file = e.target.files[0];
        if (file) {
            fileLabel.textContent = file.name;
            fileLabel.classList.add('file-selected');
            
            // Validate file size (10MB max)
            const maxSize = 10 * 1024 * 1024; // 10MB in bytes
            if (file.size > maxSize) {
                showMessage('File size exceeds 10MB limit. Please choose a smaller file.', 'error');
                fileInput.value = '';
                fileLabel.textContent = 'Choose file...';
                fileLabel.classList.remove('file-selected');
                return;
            }
            
            // Validate file type
            const allowedTypes = ['application/pdf', 'application/msword', 
                                 'application/vnd.openxmlformats-officedocument.wordprocessingml.document'];
            if (!allowedTypes.includes(file.type)) {
                showMessage('Invalid file type. Please upload a PDF, DOC, or DOCX file.', 'error');
                fileInput.value = '';
                fileLabel.textContent = 'Choose file...';
                fileLabel.classList.remove('file-selected');
                return;
            }
            
            hideMessage();
        } else {
            fileLabel.textContent = 'Choose file...';
            fileLabel.classList.remove('file-selected');
        }
    });
    
    // Handle form submission
    form.addEventListener('submit', async function(e) {
        e.preventDefault();
        
        const formData = new FormData(form);
        const file = fileInput.files[0];
        
        if (!file) {
            showMessage('Please select a resume file.', 'error');
            return;
        }
        
        // Show loading state
        submitBtn.disabled = true;
        btnText.style.display = 'none';
        btnLoader.style.display = 'inline';
        hideMessage();
        
        try {
            const response = await fetch('/api/resume/upload', {
                method: 'POST',
                body: formData
            });
            
            const result = await response.json();
            
            if (response.ok) {
                showMessage('Resume uploaded successfully!', 'success');
                form.reset();
                fileLabel.textContent = 'Choose file...';
                fileLabel.classList.remove('file-selected');
            } else {
                showMessage(result.message || 'Failed to upload resume. Please try again.', 'error');
            }
        } catch (error) {
            console.error('Upload error:', error);
            showMessage('An error occurred while uploading. Please try again.', 'error');
        } finally {
            // Reset button state
            submitBtn.disabled = false;
            btnText.style.display = 'inline';
            btnLoader.style.display = 'none';
        }
    });
}

function showMessage(message, type) {
    const messageDiv = document.getElementById('upload-message');
    messageDiv.textContent = message;
    messageDiv.className = `message message-${type}`;
    messageDiv.style.display = 'block';
}

function hideMessage() {
    const messageDiv = document.getElementById('upload-message');
    messageDiv.style.display = 'none';
}

