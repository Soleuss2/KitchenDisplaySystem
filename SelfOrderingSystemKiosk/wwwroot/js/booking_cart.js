﻿// =====================
// 🧾 Booking & Cart Logic
// =====================

const pricePerHead = 377;
let personCount = 0;
let cart = [];

// 🌐 Modal control (global functions)
function openModal(id) {
    const modal = document.getElementById(id);
    if (modal) modal.classList.add("active");
}

function closeModal(id) {
    const modal = document.getElementById(id);
    if (modal) {
        modal.classList.remove("active");
        modal.style.animation = "fadeOut 0.25s ease-in-out";
        setTimeout(() => {
            modal.style.animation = "";
        }, 250);
    }
}

// 🌟 Notification system
function showNotification(message, type = 'info', duration = 3000) {
    const container = document.getElementById('notificationContainer');
    if (!container) return;

    const notif = document.createElement('div');
    notif.textContent = message;

    notif.style.padding = '10px 15px';
    notif.style.borderRadius = '8px';
    notif.style.color = '#fff';
    notif.style.minWidth = '200px';
    notif.style.fontSize = '0.9rem';
    notif.style.boxShadow = '0 4px 12px rgba(0,0,0,0.15)';
    notif.style.opacity = '0';
    notif.style.transition = 'opacity 0.3s, transform 0.3s';
    notif.style.transform = 'translateY(-20px)';

    if (type === 'error') notif.style.backgroundColor = '#e74c3c';
    else if (type === 'success') notif.style.backgroundColor = '#28a745';
    else notif.style.backgroundColor = '#3498db';

    container.appendChild(notif);

    requestAnimationFrame(() => {
        notif.style.opacity = '1';
        notif.style.transform = 'translateY(0)';
    });

    setTimeout(() => {
        notif.style.opacity = '0';
        notif.style.transform = 'translateY(-20px)';
        setTimeout(() => container.removeChild(notif), 300);
    }, duration);
}

// ==========================
// 📏 Flavor & Quantity Limits
// ==========================
function getFlavorLimit() {
    if (personCount <= 2) return 4;
    if (personCount <= 6) return 8;
    return Infinity; // unlimited
}

function getQuantityLimit() {
    if (personCount <= 2) return 4;
    if (personCount <= 6) return 8;
    return 12;
}

// 🪟 Show person modal on page load
window.onload = function () {
    openModal("personModal");
};

// ✅ Confirm persons
document.addEventListener("DOMContentLoaded", () => {
    const confirmBtn = document.getElementById("confirmPersons");
    if (confirmBtn) {
        confirmBtn.addEventListener("click", function () {
            const input = document.getElementById("personCount").value;
            if (input < 1 || input === "") {
                showNotification("Please enter a valid number of persons.", 'error');
                return;
            }

            personCount = parseInt(input);
            document.querySelector(".person-count").textContent =
                `${personCount} Person${personCount > 1 ? "s" : ""}`;
            document.querySelector(".order-total").textContent =
                `TOTAL: ₱${(personCount * pricePerHead).toFixed(2)}`;

            closeModal("personModal");
            showRulesModal();
        });
    }

    // 🍗 Add to cart by two’s
    document.querySelectorAll('.add-to-cart').forEach(button => {
        button.addEventListener('click', function (e) {
            e.preventDefault();

            if (personCount === 0) {
                showNotification("Please enter the number of persons first.", 'error');
                openModal("personModal");
                return;
            }

            const name = this.getAttribute('data-name');
            const image = this.getAttribute('data-image');
            const flavorLimit = getFlavorLimit();

            if (cart.length >= flavorLimit && !cart.find(i => i.name === name)) {
                showNotification(`You can only choose up to ${flavorLimit} flavors.`, 'error');
                return;
            }

            const existingItem = cart.find(item => item.name === name);
            const quantityLimit = getQuantityLimit();

            if (existingItem) {
                if (existingItem.quantity >= quantityLimit) {
                    showNotification(`You can only order up to ${quantityLimit} pcs per flavor.`, 'error');
                    return;
                }
                existingItem.quantity += 2;
            } else {
                cart.push({ name, image, quantity: 2 });
            }

            updateCartDisplay();

            // ✅ Feedback animation
            this.textContent = 'Added!';
            this.style.backgroundColor = '#28a745';
            setTimeout(() => {
                this.textContent = 'Add to cart';
                this.style.backgroundColor = '';
            }, 500);
        });
    });
});

// 📋 Show the rules modal dynamically
function showRulesModal() {
    const rulesBody = document.getElementById("rulesBody");

    let message = "";
    if (personCount <= 2) {
        message = `
        <b>For 1–2 Customers:</b><br>
        • You can choose up to <b>4 flavors</b>.<br>
        • Maximum of <b>4 pcs</b> per flavor.
        `;
    } else if (personCount <= 6) {
        message = `
        <b>For 3–6 Customers:</b><br>
        • You can choose up to <b>8 flavors</b>.<br>
        • Maximum of <b>8 pcs</b> per flavor.
        `;
    } else {
        message = `
        <b>For 7+ Customers:</b><br>
        • You can choose <b>unlimited flavors</b>.<br>
        • Up to <b>12 pcs</b> per flavor.
        `;
    }

    rulesBody.innerHTML = message;
    openModal("rulesModal");
}

// 🛒 Update cart display
function updateCartDisplay() {
    const summaryList = document.querySelector('.summary-list');
    const itemCount = document.querySelector('.item-count');

    summaryList.innerHTML = '';

    if (cart.length === 0) {
        summaryList.innerHTML = `
            <div class="empty-cart">
                <i class="bi bi-cart-x-fill" style="font-size: 3rem;"></i>
                <p>Your cart is empty<br>Add items from the menu to get started</p>
            </div>
        `;
        itemCount.textContent = '0 Items';
        return;
    }

    cart.forEach((item, index) => {
        const itemDiv = document.createElement('div');
        itemDiv.className = 'cart-item';
        itemDiv.innerHTML = `
            <img src="${item.image}" alt="${item.name}"
                style="width:50px;height:50px;object-fit:cover;border-radius:8px;">
            <div style="flex:1;margin-left:10px;">
                <h5 style="margin:0;font-size:14px;">${item.name}</h5>
            </div>
            <div style="display:flex;align-items:center;gap:10px;">
                <button class="qty-btn minus" data-index="${index}">-</button>
                <span class="qty">${item.quantity}</span>
                <button class="qty-btn plus" data-index="${index}">+</button>
                <button class="remove-btn" data-index="${index}">
                    <i class="bi bi-trash"></i>
                </button>
            </div>
        `;
        summaryList.appendChild(itemDiv);
    });

    const totalItems = cart.reduce((sum, item) => sum + item.quantity, 0);
    itemCount.textContent = `${totalItems} Item${totalItems !== 1 ? 's' : ''}`;

    addCartEventListeners();
}

// 🔁 Add event listeners for cart quantity buttons
function addCartEventListeners() {
    document.querySelectorAll('.qty-btn.plus').forEach(btn => {
        btn.addEventListener('click', function () {
            const index = parseInt(this.getAttribute('data-index'));
            const quantityLimit = getQuantityLimit();

            if (cart[index].quantity >= quantityLimit) {
                showNotification(`You can only order up to ${quantityLimit} pcs per flavor.`, 'error');
                return;
            }

            cart[index].quantity += 2;
            updateCartDisplay();
        });
    });

    document.querySelectorAll('.qty-btn.minus').forEach(btn => {
        btn.addEventListener('click', function () {
            const index = parseInt(this.getAttribute('data-index'));
            if (cart[index].quantity > 2) {
                cart[index].quantity -= 2;
            } else {
                cart.splice(index, 1);
            }
            updateCartDisplay();
        });
    });

    document.querySelectorAll('.remove-btn').forEach(btn => {
        btn.addEventListener('click', function () {
            const index = parseInt(this.getAttribute('data-index'));
            cart.splice(index, 1);
            updateCartDisplay();
        });
    });
}
