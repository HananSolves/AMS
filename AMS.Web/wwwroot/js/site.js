// Token refresh functionality
let tokenRefreshInterval;

function startTokenRefresh() {
    // Refresh token every 14 minutes (1 minute before expiry)
    tokenRefreshInterval = setInterval(async () => {
        try {
            const response = await fetch('/Auth/RefreshToken', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                }
            });

            const result = await response.json();

            if (!result.success) {
                console.warn('Token refresh failed:', result.message);
                clearInterval(tokenRefreshInterval);
                window.location.href = '/Auth/Login';
            }
        } catch (error) {
            console.error('Token refresh error:', error);
        }
    }, 14 * 60 * 1000); // 14 minutes
}

// Initialize on page load
if (document.cookie.includes('AccessToken')) {
    startTokenRefresh();
}

// Form validation helpers
function validateEmail(email) {
    const re = /^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$/;
    return re.test(String(email).toLowerCase());
}

function validatePassword(password) {
    // At least 8 characters, 1 uppercase, 1 lowercase, 1 number, 1 special char
    const re = /^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[@$!%*?&#])[A-Za-z\d@$!%*?&#]{8,}$/;
    return re.test(password);
}

// Auto-dismiss alerts
document.addEventListener('DOMContentLoaded', function() {
    const alerts = document.querySelectorAll('[role="alert"]');
    alerts.forEach(alert => {
        setTimeout(() => {
            alert.style.transition = 'opacity 0.5s ease';
            alert.style.opacity = '0';
            setTimeout(() => alert.remove(), 500);
        }, 5000);
    });
});