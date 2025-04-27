/**
 * Registrar dashboard AJAX functionality
 * Handles navigation and content loading for the registrar dashboard
 */

// Main content container
const contentContainer = document.getElementById('dynamic-content');
const contentLoader = document.getElementById('content-loader');

// Setup event handlers for sidebar navigation and other interactive elements
function setupEventHandlers() {
    // Find all sidebar links with data-controller and data-action attributes
    const sidebarLinks = document.querySelectorAll('a[data-controller][data-action]');
    
    sidebarLinks.forEach(link => {
        link.addEventListener('click', function(e) {
            e.preventDefault();
            
            // Get the controller and action from data attributes
            const controller = this.getAttribute('data-controller');
            const action = this.getAttribute('data-action');
            
            // Get the venue ID if available (for detail pages)
            const venueId = this.getAttribute('data-venue-id');
            
            // Build the URL based on attributes
            let url = `/${controller}/${action}`;
            
            // Add venue ID parameter if available
            if (venueId) {
                url += `/${venueId}`;
            }
            
            // Check if AJAX loading is required
            const isAjax = this.getAttribute('data-ajax') === 'true';
            
            if (isAjax) {
                loadContentWithAjax(url);
            } else {
                // Regular navigation
                window.location.href = url;
            }
            
        });
    });
}

// Function to setup sidebar toggle
function setupSidebarToggle() {
    // This function is already implemented in the _RegistrarLayout.cshtml
    // This is just a placeholder to prevent errors
}

// Load content via AJAX
function loadContentWithAjax(url) {
    // Make AJAX request
    fetch(url, {
        headers: {
            'X-Requested-With': 'XMLHttpRequest'
        }
    })
    .then(response => {
        if (!response.ok) {
            throw new Error('Network response was not ok');
        }
        return response.text();
    })
    .then(html => {
        // Update content
        if (contentContainer) {
            contentContainer.innerHTML = html;
        }
        
        // Update browser history
        window.history.pushState({ path: url }, '', url);
        
        // Re-initialize any scripts needed for the new content
        initializeLoadedScripts();
        
        // Highlight active sidebar item
        highlightActiveSidebarItem();
    })
    .catch(error => {
        console.error('Error loading content:', error);
        
        // Show error message
        if (contentContainer) {
            contentContainer.innerHTML = `
                <div class="bg-red-100 dark:bg-red-900/30 border border-red-200 dark:border-red-800 text-red-700 dark:text-red-400 px-4 py-3 rounded relative mb-4">
                    <strong class="font-bold">Error!</strong>
                    <span class="block sm:inline"> Failed to load content. Please try again later.</span>
                </div>
            `;
        }
    });
}

// Highlight active sidebar item based on current URL
function highlightActiveSidebarItem() {
    const currentAction = window.location.pathname.split('/').pop();
    const sidebarLinks = document.querySelectorAll('nav a[data-action]');
    
    sidebarLinks.forEach(link => {
        // Remove active class from all links
        link.classList.remove('sidebar-active');
        
        // Add active class to matching link
        const action = link.getAttribute('data-action');
        if (action === currentAction) {
            link.classList.add('sidebar-active');
        }
    });
}

// Initialize dynamic content scripts
function initializeLoadedScripts() {
    // Re-attach event handlers for interactive elements in the loaded content
    
    // Process buttons that trigger AJAX actions
    const actionButtons = document.querySelectorAll('[data-ajax-action]');
    actionButtons.forEach(button => {
        button.addEventListener('click', function(e) {
            e.preventDefault();
            
            const url = this.getAttribute('data-ajax-url');
            if (url) {
                loadContentWithAjax(url);
            }
        });
    });
    
    // Process forms that should be submitted via AJAX
    const ajaxForms = document.querySelectorAll('form[data-ajax="true"]');
    ajaxForms.forEach(form => {
        form.addEventListener('submit', function(e) {
            e.preventDefault();
            
            const url = this.getAttribute('action');
            const method = this.getAttribute('method') || 'POST';
            
            // Show loader
            if (contentLoader) {
                contentLoader.classList.remove('hidden');
            }
            
            fetch(url, {
                method: method,
                headers: {
                    'X-Requested-With': 'XMLHttpRequest'
                },
                body: new FormData(this)
            })
            .then(response => {
                if (!response.ok) {
                    throw new Error('Network response was not ok');
                }
                return response.text();
            })
            .then(html => {
                // Update content
                if (contentContainer) {
                    contentContainer.innerHTML = html;
                }
                
                // Hide loader
                if (contentLoader) {
                    contentLoader.classList.add('hidden');
                }
                
                // Re-initialize content scripts
                initializeLoadedScripts();
            })
            .catch(error => {
                console.error('Error submitting form:', error);
                
                // Hide loader
                if (contentLoader) {
                    contentLoader.classList.add('hidden');
                }
                
                // Show error message
                const errorDiv = document.createElement('div');
                errorDiv.className = 'bg-red-100 dark:bg-red-900/30 border border-red-200 dark:border-red-800 text-red-700 dark:text-red-400 px-4 py-3 rounded relative mb-4';
                errorDiv.innerHTML = `
                    <strong class="font-bold">Error!</strong>
                    <span class="block sm:inline"> Failed to submit form. Please try again later.</span>
                `;
                form.prepend(errorDiv);
            });
        });
    });
}

// Setup theme switching
function setupThemeSwitching() {
    // Already implemented in the main layout
    // This is just a placeholder to prevent errors
}

// Handle browser back/forward navigation
window.addEventListener('popstate', function(event) {
    if (event.state && event.state.path) {
        loadContentWithAjax(event.state.path);
    } else {
        loadContentWithAjax(window.location.href);
    }
});

// Initialize on document ready
document.addEventListener('DOMContentLoaded', function() {
    // Setup event handlers for navigation
    setupEventHandlers();
    
    // Setup sidebar toggle
    setupSidebarToggle();
    
    // Initialize scripts for the initial page load
    initializeLoadedScripts();
    
    // Highlight active sidebar item based on current URL
    highlightActiveSidebarItem();
    
    // Store initial state for browser history
    window.history.replaceState({ path: window.location.href }, '', window.location.href);
}); 