/**
 * Customer panel AJAX functionality
 * Handles loading content dynamically without full page refreshes
 */

// Main content container where AJAX loaded content will be displayed
const mainContentContainer = document.getElementById('customer-main-content');

// Function to load content via AJAX
function loadContent(url, pushState = true) {
    // Don't use AJAX for BrowseVenue as per requirements
    if (url.includes('/Venues/Browse')) {
        window.location.href = url;
        return;
    }
    
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
        mainContentContainer.innerHTML = html;
        
        // Update browser history
        if (pushState) {
            window.history.pushState({ path: url }, '', url);
        }
        
        // Re-initialize any scripts needed for the new content
        initializeContentScripts();
        
        // Highlight active sidebar item
        highlightActiveSidebarItem(url);
        
        // Add fade-in animation to the loaded content
        mainContentContainer.classList.add('page-transition-enter');
        setTimeout(() => {
            mainContentContainer.classList.remove('page-transition-enter');
            mainContentContainer.classList.add('page-transition-enter-active');
            
            setTimeout(() => {
                mainContentContainer.classList.remove('page-transition-enter-active');
            }, 300);
        }, 10);
    })
    .catch(error => {
        console.error('Error loading content:', error);
        // Show error message
        mainContentContainer.innerHTML = `
            <div class="bg-red-100 dark:bg-red-900/30 border border-red-200 dark:border-red-800 text-red-700 dark:text-red-400 px-4 py-3 rounded relative mb-4">
                <strong class="font-bold">Error!</strong>
                <span class="block sm:inline"> Failed to load content. Please try again later.</span>
            </div>
        `;
    });
}

// Handle browser back/forward navigation
window.addEventListener('popstate', function(event) {
    if (event.state && event.state.path) {
        loadContent(event.state.path, false);
    } else {
        loadContent(window.location.href, false);
    }
});

// Highlight active sidebar item based on current URL
function highlightActiveSidebarItem(url) {
    const sidebarLinks = document.querySelectorAll('nav a');
    
    sidebarLinks.forEach(link => {
        link.classList.remove('sidebar-active');
        
        // Check if the href attribute of the link matches the current URL
        if (link.getAttribute('href') === url || url.includes(link.getAttribute('href'))) {
            link.classList.add('sidebar-active');
        }
    });
}

// Initialize scripts for newly loaded content
function initializeContentScripts() {
    // Initialize form submissions
    const forms = mainContentContainer.querySelectorAll('form:not([data-ajax="false"])');
    
    forms.forEach(form => {
        form.addEventListener('submit', function(e) {
            e.preventDefault();
            
            const url = form.getAttribute('action') || window.location.href;
            const method = form.getAttribute('method') || 'POST';
            
            fetch(url, {
                method: method,
                headers: {
                    'X-Requested-With': 'XMLHttpRequest'
                },
                body: new FormData(form)
            })
            .then(response => {
                if (!response.ok) {
                    throw new Error('Network response was not ok');
                }
                return response.text();
            })
            .then(html => {
                // Update content
                mainContentContainer.innerHTML = html;
                
                // Re-initialize content scripts
                initializeContentScripts();
            })
            .catch(error => {
                console.error('Error submitting form:', error);
                // Show error message at the top of the form
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
    
    // Initialize any click events for things like pagination, sorting, etc.
    const contentLinks = mainContentContainer.querySelectorAll('a:not([data-ajax="false"])');
    
    contentLinks.forEach(link => {
        // Skip links that should perform regular navigation
        if (link.getAttribute('href').startsWith('#') || 
            link.getAttribute('href').startsWith('http') || 
            link.getAttribute('href').includes('/Venues/Browse') ||
            link.getAttribute('target') === '_blank') {
            return;
        }
        
        link.addEventListener('click', function(e) {
            e.preventDefault();
            const url = this.getAttribute('href');
            loadContent(url);
        });
    });
}

// Initialize AJAX navigation when the DOM is loaded
document.addEventListener('DOMContentLoaded', function() {
    // Wrap main content in a container for AJAX loading if not already wrapped
    if (!mainContentContainer) {
        const mainElement = document.querySelector('main');
        const wrapper = document.createElement('div');
        wrapper.id = 'customer-main-content';
        wrapper.className = 'w-full';
        
        // Move all main content into the wrapper
        while (mainElement.firstChild) {
            wrapper.appendChild(mainElement.firstChild);
        }
        
        // Append the wrapper to main
        mainElement.appendChild(wrapper);
    }
    
    // Set up click handlers for navigation links
    document.querySelectorAll('nav a').forEach(link => {
        // Skip links that should perform regular navigation
        if (link.getAttribute('href').startsWith('#') || 
            link.getAttribute('href').startsWith('http') || 
            link.getAttribute('href').includes('/Venues/Browse') ||
            link.getAttribute('target') === '_blank' ||
            link.classList.contains('nav-no-ajax')) {
            return;
        }
        
        link.addEventListener('click', function(e) {
            e.preventDefault();
            const url = this.getAttribute('href');
            loadContent(url);
        });
    });
    
    // Initialize content scripts for the initial page load
    initializeContentScripts();
    
    // Store initial state
    window.history.replaceState({ path: window.location.href }, '', window.location.href);
}); 