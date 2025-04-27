// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.

// Parallax scroll effect
document.addEventListener('DOMContentLoaded', function() {
    // Apply parallax effect to elements with 'parallax' class
    const parallaxElements = document.querySelectorAll('.parallax');
    
    if (parallaxElements.length > 0) {
        window.addEventListener('scroll', function() {
            parallaxElements.forEach(element => {
                const scrollPosition = window.pageYOffset;
                const distance = element.getBoundingClientRect().top + scrollPosition;
                const elementHeight = element.offsetHeight;
                const viewportHeight = window.innerHeight;
                
                // Check if element is in viewport
                if (distance < scrollPosition + viewportHeight && distance + elementHeight > scrollPosition) {
                    const speed = 0.3; // Parallax speed (adjust as needed)
                    const yPos = (scrollPosition - distance) * speed;
                    element.style.backgroundPositionY = `${yPos}px`;
                }
            });
        });
    }
    
    // Ripple effect
    const rippleButtons = document.querySelectorAll('.btn-ripple');
    
    rippleButtons.forEach(button => {
        button.addEventListener('click', function(e) {
            const x = e.clientX - e.target.getBoundingClientRect().left;
            const y = e.clientY - e.target.getBoundingClientRect().top;
            
            const circle = document.createElement('span');
            circle.classList.add('ripple-effect');
            circle.style.left = `${x}px`;
            circle.style.top = `${y}px`;
            
            this.appendChild(circle);
            
            setTimeout(() => {
                circle.remove();
            }, 600);
        });
    });
    
    // Smooth scroll for anchor links
    document.querySelectorAll('a[href^="#"]').forEach(anchor => {
        anchor.addEventListener('click', function(e) {
            e.preventDefault();
            
            const targetId = this.getAttribute('href');
            
            if (targetId === '#') return;
            
            const targetElement = document.querySelector(targetId);
            
            if (targetElement) {
                window.scrollTo({
                    top: targetElement.offsetTop - 80, // Adjust for header height
                    behavior: 'smooth'
                });
                
                // Update URL hash without jumping
                history.pushState(null, null, targetId);
            }
        });
    });
    
    // Fade in elements on scroll
    const fadeElements = document.querySelectorAll('.fade-in-on-scroll');
    
    function checkFadeElements() {
        fadeElements.forEach(element => {
            const elementTop = element.getBoundingClientRect().top;
            const elementBottom = element.getBoundingClientRect().bottom;
            
            // Check if element is in viewport
            if (elementTop < window.innerHeight - 100 && elementBottom > 0) {
                element.classList.add('visible');
            }
        });
    }
    
    if (fadeElements.length > 0) {
        // Initial check in case elements are already in view
        checkFadeElements();
        
        // Check on scroll
        window.addEventListener('scroll', checkFadeElements);
    }
    
    // Testimonial Carousel
    const carousel = document.getElementById('testimonialCarousel');
    
    if (carousel) {
        const slides = document.querySelectorAll('.testimonial-slide');
        const prevButton = document.getElementById('prevButton');
        const nextButton = document.getElementById('nextButton');
        const indicators = document.querySelectorAll('.carousel-indicator');
        
        let currentSlide = 0;
        const slideCount = slides.length;
        let intervalId;
        
        function updateCarousel() {
            const offset = currentSlide * -100;
            carousel.style.transform = `translateX(${offset}%)`;
            
            // Update indicators
            indicators.forEach((indicator, index) => {
                if (index === currentSlide) {
                    indicator.classList.remove('bg-primary-200', 'dark:bg-primary-800');
                    indicator.classList.add('bg-primary-500', 'dark:bg-primary-400');
                } else {
                    indicator.classList.remove('bg-primary-500', 'dark:bg-primary-400');
                    indicator.classList.add('bg-primary-200', 'dark:bg-primary-800');
                }
            });
        }
        
        function nextSlide() {
            currentSlide = (currentSlide + 1) % slideCount;
            updateCarousel();
        }
        
        function prevSlide() {
            currentSlide = (currentSlide - 1 + slideCount) % slideCount;
            updateCarousel();
        }
        
        // Set up event listeners
        if (nextButton && prevButton) {
            nextButton.addEventListener('click', () => {
                nextSlide();
                resetAutoSlide();
            });
            
            prevButton.addEventListener('click', () => {
                prevSlide();
                resetAutoSlide();
            });
        }
        
        // Set up indicator click events
        indicators.forEach((indicator, index) => {
            indicator.addEventListener('click', () => {
                currentSlide = index;
                updateCarousel();
                resetAutoSlide();
            });
        });
        
        function startAutoSlide() {
            intervalId = setInterval(nextSlide, 5000);
        }
        
        function resetAutoSlide() {
            clearInterval(intervalId);
            startAutoSlide();
        }
        
        // Start auto-advance
        startAutoSlide();
        
        // Pause auto-advance on hover
        carousel.addEventListener('mouseenter', () => clearInterval(intervalId));
        carousel.addEventListener('mouseleave', startAutoSlide);
        
        let touchStartX = 0;
        let touchEndX = 0;
        
        carousel.addEventListener('touchstart', e => {
            touchStartX = e.changedTouches[0].screenX;
        });
        
        carousel.addEventListener('touchend', e => {
            touchEndX = e.changedTouches[0].screenX;
            handleSwipe();
        });
        
        function handleSwipe() {
            // Detect swipe direction (threshold of 50px)
            if (touchEndX < touchStartX - 50) {
                nextSlide(); // Swipe left
                resetAutoSlide();
            } else if (touchEndX > touchStartX + 50) {
                prevSlide(); // Swipe right
                resetAutoSlide();
            }
        }
    }
});
