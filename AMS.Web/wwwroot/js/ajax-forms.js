// wwwroot/js/ajax-forms.js
// Handles all AJAX form submissions to prevent page refreshes

/**
 * Submit form via AJAX
 * @param {HTMLFormElement} form - The form element to submit
 * @param {Function} onSuccess - Callback function on success
 * @param {Function} onError - Callback function on error
 */
async function submitFormAjax(form, onSuccess, onError) {
    try {
        const formData = new FormData(form);
        const action = form.action;
        const method = form.method || 'POST';

        const response = await fetch(action, {
            method: method,
            body: formData,
            headers: {
                'X-Requested-With': 'XMLHttpRequest'
            }
        });

        if (response.ok) {
            // Check if response is JSON or HTML
            const contentType = response.headers.get('content-type');
            if (contentType && contentType.includes('application/json')) {
                const data = await response.json();
                if (onSuccess) onSuccess(data, response);
            } else {
                // If HTML response, redirect was likely intended
                if (response.redirected) {
                    window.location.href = response.url;
                } else {
                    if (onSuccess) onSuccess(null, response);
                }
            }
        } else {
            const errorText = await response.text();
            if (onError) onError(errorText, response);
        }
    } catch (error) {
        console.error('AJAX submission error:', error);
        if (onError) onError(error.message);
    }
}

/**
 * Show toast notification
 * @param {string} message - The message to display
 * @param {string} type - success, error, warning, info
 */
function showToast(message, type = 'success') {
    // Remove any existing toasts
    const existingToast = document.querySelector('.toast-notification');
    if (existingToast) {
        existingToast.remove();
    }

    const toast = document.createElement('div');
    toast.className = `toast-notification toast-${type}`;

    const colors = {
        success: 'bg-green-500',
        error: 'bg-red-500',
        warning: 'bg-yellow-500',
        info: 'bg-blue-500'
    };

    const icons = {
        success: '✓',
        error: '✕',
        warning: '⚠',
        info: 'ℹ'
    };

    toast.innerHTML = `
        <div class="fixed top-4 right-4 ${colors[type]} text-white px-6 py-4 rounded-lg shadow-lg z-50 flex items-center space-x-3 animate-slide-in">
            <span class="text-xl font-bold">${icons[type]}</span>
            <span>${message}</span>
        </div>
    `;

    document.body.appendChild(toast);

    // Auto-remove after 3 seconds
    setTimeout(() => {
        toast.classList.add('animate-slide-out');
        setTimeout(() => toast.remove(), 300);
    }, 3000);
}

/**
 * Handle course creation form
 */
function initCourseCreateForm() {
    const form = document.getElementById('courseCreateForm');
    if (!form) return;

    form.addEventListener('submit', async (e) => {
        e.preventDefault();

        const submitBtn = form.querySelector('button[type="submit"]');
        const originalText = submitBtn.innerHTML;
        submitBtn.disabled = true;
        submitBtn.innerHTML = '<span class="inline-block animate-spin mr-2">⟳</span>Creating...';

        await submitFormAjax(
            form,
            (data, response) => {
                showToast('Course created successfully!', 'success');
                submitBtn.disabled = false;
                submitBtn.innerHTML = originalText;

                // Redirect to courses list after short delay
                setTimeout(() => {
                    window.location.href = '/Course/Index';
                }, 1000);
            },
            (error) => {
                showToast('Failed to create course. Please try again.', 'error');
                submitBtn.disabled = false;
                submitBtn.innerHTML = originalText;
            }
        );
    });
}

/**
 * Handle course edit form
 */
function initCourseEditForm() {
    const form = document.getElementById('courseEditForm');
    if (!form) return;

    form.addEventListener('submit', async (e) => {
        e.preventDefault();

        const submitBtn = form.querySelector('button[type="submit"]');
        const originalText = submitBtn.innerHTML;
        submitBtn.disabled = true;
        submitBtn.innerHTML = '<span class="inline-block animate-spin mr-2">⟳</span>Updating...';

        await submitFormAjax(
            form,
            (data, response) => {
                showToast('Course updated successfully!', 'success');
                submitBtn.disabled = false;
                submitBtn.innerHTML = originalText;

                // Redirect to courses list after short delay
                setTimeout(() => {
                    window.location.href = '/Course/Index';
                }, 1000);
            },
            (error) => {
                showToast('Failed to update course. Please try again.', 'error');
                submitBtn.disabled = false;
                submitBtn.innerHTML = originalText;
            }
        );
    });
}

/**
 * Handle enrollment actions
 */
function initEnrollmentActions() {
    // Enroll buttons
    document.querySelectorAll('.enroll-btn').forEach(btn => {
        btn.addEventListener('click', async (e) => {
            e.preventDefault();
            const courseId = btn.dataset.courseId;
            const originalText = btn.innerHTML;

            btn.disabled = true;
            btn.innerHTML = '<span class="inline-block animate-spin mr-2">⟳</span>Enrolling...';

            try {
                const response = await fetch('/Enrollment/Enroll', {
                    method: 'POST',
                    headers: {
                        'Content-Type': 'application/x-www-form-urlencoded',
                        'X-Requested-With': 'XMLHttpRequest'
                    },
                    body: `courseId=${courseId}`
                });

                if (response.ok) {
                    showToast('Successfully enrolled in course!', 'success');

                    // Update button to "Unenroll"
                    btn.classList.remove('enroll-btn', 'btn-primary');
                    btn.classList.add('unenroll-btn', 'btn-secondary');
                    btn.innerHTML = 'Unenroll';
                    btn.disabled = false;

                    // Re-initialize to handle unenroll
                    initEnrollmentActions();
                } else {
                    showToast('Failed to enroll. Please try again.', 'error');
                    btn.disabled = false;
                    btn.innerHTML = originalText;
                }
            } catch (error) {
                showToast('An error occurred. Please try again.', 'error');
                btn.disabled = false;
                btn.innerHTML = originalText;
            }
        });
    });

    // Unenroll buttons
    document.querySelectorAll('.unenroll-btn').forEach(btn => {
        btn.addEventListener('click', async (e) => {
            e.preventDefault();
            const courseId = btn.dataset.courseId;
            const originalText = btn.innerHTML;

            btn.disabled = true;
            btn.innerHTML = '<span class="inline-block animate-spin mr-2">⟳</span>Unenrolling...';

            try {
                const response = await fetch('/Enrollment/Unenroll', {
                    method: 'POST',
                    headers: {
                        'Content-Type': 'application/x-www-form-urlencoded',
                        'X-Requested-With': 'XMLHttpRequest'
                    },
                    body: `courseId=${courseId}`
                });

                if (response.ok) {
                    showToast('Successfully unenrolled from course!', 'success');

                    // Update button to "Enroll"
                    btn.classList.remove('unenroll-btn', 'btn-secondary');
                    btn.classList.add('enroll-btn', 'btn-primary');
                    btn.innerHTML = 'Enroll';
                    btn.disabled = false;

                    // Re-initialize to handle enroll
                    initEnrollmentActions();
                } else {
                    showToast('Failed to unenroll. Please try again.', 'error');
                    btn.disabled = false;
                    btn.innerHTML = originalText;
                }
            } catch (error) {
                showToast('An error occurred. Please try again.', 'error');
                btn.disabled = false;
                btn.innerHTML = originalText;
            }
        });
    });
}

/**
 * Handle course deletion with confirmation
 */
function initCourseDeleteActions() {
    document.querySelectorAll('.delete-course-btn').forEach(btn => {
        btn.addEventListener('click', async (e) => {
            e.preventDefault();

            if (!confirm('Are you sure you want to delete this course?')) {
                return;
            }

            const form = btn.closest('form');
            const courseId = form.querySelector('input[name="id"]')?.value;

            try {
                const response = await fetch(form.action, {
                    method: 'POST',
                    headers: {
                        'Content-Type': 'application/x-www-form-urlencoded',
                        'X-Requested-With': 'XMLHttpRequest'
                    },
                    body: `id=${courseId}`
                });

                if (response.ok) {
                    showToast('Course deleted successfully!', 'success');

                    // Remove the course row from the table
                    const row = btn.closest('tr');
                    if (row) {
                        row.style.opacity = '0';
                        row.style.transition = 'opacity 0.3s';
                        setTimeout(() => row.remove(), 300);
                    }
                } else {
                    showToast('Failed to delete course. Please try again.', 'error');
                }
            } catch (error) {
                showToast('An error occurred. Please try again.', 'error');
            }
        });
    });
}

/**
 * Handle attendance marking form
 */
function initAttendanceMarkForm() {
    const form = document.getElementById('attendanceMarkForm');
    if (!form) return;

    form.addEventListener('submit', async (e) => {
        e.preventDefault();

        const submitBtn = form.querySelector('button[type="submit"]');
        const originalText = submitBtn.innerHTML;
        submitBtn.disabled = true;
        submitBtn.innerHTML = '<span class="inline-block animate-spin mr-2">⟳</span>Marking...';

        await submitFormAjax(
            form,
            (data, response) => {
                showToast('Attendance marked successfully!', 'success');
                submitBtn.disabled = false;
                submitBtn.innerHTML = originalText;

                // Redirect to dashboard after short delay
                setTimeout(() => {
                    window.location.href = '/Dashboard/Index';
                }, 1000);
            },
            (error) => {
                showToast('Failed to mark attendance. Please try again.', 'error');
                submitBtn.disabled = false;
                submitBtn.innerHTML = originalText;
            }
        );
    });
}

/**
 * Handle login form
 */
function initLoginForm() {
    const form = document.getElementById('loginForm');
    if (!form) return;

    form.addEventListener('submit', async (e) => {
        e.preventDefault();

        const submitBtn = form.querySelector('button[type="submit"]');
        const originalText = submitBtn.innerHTML;
        submitBtn.disabled = true;
        submitBtn.innerHTML = '<span class="inline-block animate-spin mr-2">⟳</span>Signing in...';

        await submitFormAjax(
            form,
            (data, response) => {
                showToast('Login successful! Redirecting...', 'success');
                // Allow the redirect to happen naturally from the response
            },
            (error) => {
                showToast('Login failed. Please check your credentials.', 'error');
                submitBtn.disabled = false;
                submitBtn.innerHTML = originalText;
            }
        );
    });
}

/**
 * Handle register form
 */
function initRegisterForm() {
    const form = document.getElementById('registerForm');
    if (!form) return;

    form.addEventListener('submit', async (e) => {
        e.preventDefault();

        const submitBtn = form.querySelector('button[type="submit"]');
        const originalText = submitBtn.innerHTML;
        submitBtn.disabled = true;
        submitBtn.innerHTML = '<span class="inline-block animate-spin mr-2">⟳</span>Creating account...';

        await submitFormAjax(
            form,
            (data, response) => {
                showToast('Registration successful! Redirecting...', 'success');
                // Allow the redirect to happen naturally from the response
            },
            (error) => {
                showToast('Registration failed. Please check your information.', 'error');
                submitBtn.disabled = false;
                submitBtn.innerHTML = originalText;
            }
        );
    });
}

/**
 * Handle user create form
 */
function initUserCreateForm() {
    const form = document.getElementById('userCreateForm');
    if (!form) return;

    form.addEventListener('submit', async (e) => {
        e.preventDefault();

        const submitBtn = form.querySelector('button[type="submit"]');
        const originalText = submitBtn.innerHTML;
        submitBtn.disabled = true;
        submitBtn.innerHTML = '<span class="inline-block animate-spin mr-2">⟳</span>Creating...';

        await submitFormAjax(
            form,
            (data, response) => {
                showToast('User created successfully!', 'success');
                setTimeout(() => {
                    window.location.href = '/Users/Index';
                }, 1000);
            },
            (error) => {
                showToast('Failed to create user. Please try again.', 'error');
                submitBtn.disabled = false;
                submitBtn.innerHTML = originalText;
            }
        );
    });
}

/**
 * Handle user edit form
 */
function initUserEditForm() {
    const form = document.getElementById('userEditForm');
    if (!form) return;

    form.addEventListener('submit', async (e) => {
        e.preventDefault();

        const submitBtn = form.querySelector('button[type="submit"]');
        const originalText = submitBtn.innerHTML;
        submitBtn.disabled = true;
        submitBtn.innerHTML = '<span class="inline-block animate-spin mr-2">⟳</span>Updating...';

        await submitFormAjax(
            form,
            (data, response) => {
                showToast('User updated successfully!', 'success');
                setTimeout(() => {
                    window.location.href = '/Users/Index';
                }, 1000);
            },
            (error) => {
                showToast('Failed to update user. Please try again.', 'error');
                submitBtn.disabled = false;
                submitBtn.innerHTML = originalText;
            }
        );
    });
}

/**
 * Handle user deletion
 */
function initUserDeleteActions() {
    document.querySelectorAll('.user-delete-btn').forEach(btn => {
        btn.addEventListener('click', async (e) => {
            e.preventDefault();

            const userId = btn.dataset.userId;
            const userName = btn.dataset.userName || 'this user';

            if (!confirm(`Are you sure you want to delete ${userName}?`)) {
                return;
            }

            try {
                const response = await fetch('/Users/Delete', {
                    method: 'POST',
                    headers: {
                        'Content-Type': 'application/x-www-form-urlencoded',
                        'X-Requested-With': 'XMLHttpRequest'
                    },
                    body: `id=${userId}`
                });

                if (response.ok) {
                    showToast('User deleted successfully!', 'success');

                    // Remove the user row from the table
                    const row = btn.closest('tr');
                    if (row) {
                        row.style.opacity = '0';
                        row.style.transition = 'opacity 0.3s';
                        setTimeout(() => row.remove(), 300);
                    }
                } else {
                    showToast('Failed to delete user. Please try again.', 'error');
                }
            } catch (error) {
                showToast('An error occurred. Please try again.', 'error');
            }
        });
    });
}

// Initialize all forms when DOM is ready
// Initialize all forms when DOM is ready
document.addEventListener('DOMContentLoaded', () => {
    initLoginForm();
    initRegisterForm();
    initCourseCreateForm();
    initCourseEditForm();
    initEnrollmentActions();
    initCourseDeleteActions();
    initAttendanceMarkForm();
    initUserCreateForm();
    initUserEditForm();
    initUserDeleteActions();
});

// Add custom CSS for animations
const style = document.createElement('style');
style.textContent = `
    @keyframes slide-in {
        from {
            transform: translateX(100%);
            opacity: 0;
        }
        to {
            transform: translateX(0);
            opacity: 1;
        }
    }

    @keyframes slide-out {
        from {
            transform: translateX(0);
            opacity: 1;
        }
        to {
            transform: translateX(100%);
            opacity: 0;
        }
    }

    .animate-slide-in {
        animation: slide-in 0.3s ease-out;
    }

    .animate-slide-out {
        animation: slide-out 0.3s ease-in;
    }

    @keyframes spin {
        from {
            transform: rotate(0deg);
        }
        to {
            transform: rotate(360deg);
        }
    }

    .animate-spin {
        animation: spin 1s linear infinite;
    }
`;
document.head.appendChild(style);