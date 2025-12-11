// product-filter.js - Xử lý bộ lọc sản phẩm động

document.addEventListener('DOMContentLoaded', function () {
    // Lấy các elements
    const sortSelect = document.getElementById('sortSelect');
    const priceFilter = document.getElementById('priceFilter');
    const categoryFilters = document.querySelectorAll('.category-filter');
    const minPriceInput = document.getElementById('minPrice');
    const maxPriceInput = document.getElementById('maxPrice');
    const applyPriceBtn = document.getElementById('applyPrice');
    const resetFilterBtn = document.getElementById('resetFilter');

    // Lấy URL hiện tại
    const currentUrl = new URL(window.location.href);
    const params = new URLSearchParams(currentUrl.search);

    // Xử lý thay đổi sắp xếp
    if (sortSelect) {
        sortSelect.addEventListener('change', function () {
            updateFilter('sort', this.value);
        });
    }

    // Xử lý bộ lọc khoảng giá nhanh
    if (priceFilter) {
        const priceOptions = priceFilter.querySelectorAll('input[type="radio"]');
        priceOptions.forEach(option => {
            option.addEventListener('change', function () {
                const [min, max] = this.value.split('-');
                updateFilter('minPrice', min);
                updateFilter('maxPrice', max || '');
            });
        });
    }

    // Xử lý bộ lọc loại sản phẩm
    categoryFilters.forEach(filter => {
        filter.addEventListener('click', function (e) {
            e.preventDefault();
            const loaiId = this.dataset.loaiId;
            updateFilter('loai', loaiId);
        });
    });

    // Xử lý nhập giá tùy chỉnh
    if (applyPriceBtn) {
        applyPriceBtn.addEventListener('click', function () {
            const min = minPriceInput.value;
            const max = maxPriceInput.value;

            // Validate
            if (min && max && parseFloat(min) > parseFloat(max)) {
                alert('Giá từ phải nhỏ hơn giá đến!');
                return;
            }

            updateFilter('minPrice', min);
            updateFilter('maxPrice', max);
        });
    }

    // Reset bộ lọc
    if (resetFilterBtn) {
        resetFilterBtn.addEventListener('click', function () {
            // Xóa tất cả filter params
            params.delete('sort');
            params.delete('minPrice');
            params.delete('maxPrice');
            params.delete('loai');

            // Giữ lại search nếu có
            const newUrl = params.toString() ?
                `${currentUrl.pathname}?${params.toString()}` :
                currentUrl.pathname;

            window.location.href = newUrl;
        });
    }

    // Function cập nhật filter
    function updateFilter(key, value) {
        if (value && value !== '' && value !== '0') {
            params.set(key, value);
        } else {
            params.delete(key);
        }

        // Reset về trang 1 khi filter
        params.set('page', '1');

        // Reload với params mới
        window.location.href = `${currentUrl.pathname}?${params.toString()}`;
    }

    // Lazy loading images
    if ('IntersectionObserver' in window) {
        const imageObserver = new IntersectionObserver((entries, observer) => {
            entries.forEach(entry => {
                if (entry.isIntersecting) {
                    const img = entry.target;
                    const src = img.dataset.src;

                    if (src) {
                        // Tải ảnh thật
                        img.src = src;
                        img.classList.remove('lazy');
                        img.classList.add('loaded');
                        observer.unobserve(img);
                    }
                }
            });
        }, {
            rootMargin: '50px 0px',
            threshold: 0.01
        });

        // Observe tất cả ảnh có class lazy
        document.querySelectorAll('img.lazy').forEach(img => {
            imageObserver.observe(img);
        });
    }

    // WebP detection và fallback
    function supportsWebP() {
        const canvas = document.createElement('canvas');
        if (canvas.getContext && canvas.getContext('2d')) {
            return canvas.toDataURL('image/webp').indexOf('data:image/webp') === 0;
        }
        return false;
    }

    // Thay đổi extension nếu browser hỗ trợ WebP
    if (supportsWebP()) {
        document.querySelectorAll('img[data-webp]').forEach(img => {
            img.src = img.dataset.webp;
        });
    }

    // Xử lý loading state khi filter
    function showLoadingState() {
        const productGrid = document.querySelector('.product-grid');
        if (productGrid) {
            productGrid.classList.add('loading');
            productGrid.style.opacity = '0.5';
        }
    }

    // Gọi khi bắt đầu filter
    const filterForm = document.getElementById('filterForm');
    if (filterForm) {
        filterForm.addEventListener('submit', showLoadingState);
    }
});

// AJAX filter (nếu không muốn reload trang)
async function filterProductsAjax(filters) {
    try {
        const params = new URLSearchParams(filters);
        const response = await fetch(`/HangHoa/FilterProducts?${params.toString()}`);

        if (!response.ok) {
            throw new Error('Network response was not ok');
        }

        const products = await response.json();
        renderProducts(products);
    } catch (error) {
        console.error('Error filtering products:', error);
    }
}

// Render sản phẩm sau khi filter
function renderProducts(products) {
    const productGrid = document.querySelector('.product-grid');

    if (!productGrid) return;

    productGrid.innerHTML = products.map(product => `
        <div class="product-card">
            <a href="/san-pham/${product.slug}-${product.maHh}">
                <img class="lazy" 
                     data-src="/Hinh/HangHoa/${product.hinh}" 
                     src="/images/placeholder.jpg"
                     alt="${product.tenHH}"
                     loading="lazy">
            </a>
            <h3 class="product-name">
                <a href="/san-pham/${product.slug}-${product.maHh}">${product.tenHH}</a>
            </h3>
            <p class="product-price">${product.donGia.toLocaleString('vi-VN')} ₫</p>
            <button class="btn-add-cart" data-id="${product.maHh}">
                <i class="fa fa-shopping-cart"></i> Mua
            </button>
        </div>
    `).join('');

    // Re-init lazy loading cho ảnh mới
    initLazyLoading();
}

// Initialize lazy loading
function initLazyLoading() {
    if ('IntersectionObserver' in window) {
        const imageObserver = new IntersectionObserver((entries, observer) => {
            entries.forEach(entry => {
                if (entry.isIntersecting) {
                    const img = entry.target;
                    img.src = img.dataset.src;
                    img.classList.remove('lazy');
                    observer.unobserve(img);
                }
            });
        });

        document.querySelectorAll('img.lazy').forEach(img => {
            imageObserver.observe(img);
        });
    }
}

// Debounce function cho price input
function debounce(func, wait) {
    let timeout;
    return function executedFunction(...args) {
        const later = () => {
            clearTimeout(timeout);
            func(...args);
        };
        clearTimeout(timeout);
        timeout = setTimeout(later, wait);
    };
}

// Export nếu cần dùng ở file khác
if (typeof module !== 'undefined' && module.exports) {
    module.exports = { filterProductsAjax, renderProducts, debounce };
}