document.addEventListener("DOMContentLoaded", () => {
    let cart = [];
    const cartContainer = document.querySelector(".summary-list");
    const itemCount = document.querySelector(".item-count");
    const totalDisplay = document.querySelector(".order-total");
    const emptyCartMsg = document.querySelector(".empty-cart");

    // 🟠 Add to Cart
    document.querySelectorAll(".add-to-cart").forEach(btn => {
        btn.addEventListener("click", () => {
            const name = btn.dataset.name;
            const price = parseFloat(btn.dataset.price);
            const image = btn.dataset.image;

            const existing = cart.find(item => item.name === name);
            if (existing) {
                existing.quantity++;
            } else {
                cart.push({ name, price, image, quantity: 1, special: "", showInstruction: false });
            }
            renderCart();
        });
    });

    // 🟠 Render Cart
    function renderCart() {
        cartContainer.innerHTML = "";
        if (cart.length === 0) {
            emptyCartMsg.style.display = "block";
            itemCount.textContent = "0 Items";
            totalDisplay.textContent = "TOTAL: ₱0.00";
            return;
        }

        emptyCartMsg.style.display = "none";
        let total = 0;

        cart.forEach((item, index) => {
            total += item.price * item.quantity;

            const itemDiv = document.createElement("div");
            itemDiv.classList.add("cart-item");
            itemDiv.innerHTML = `
                <div class="cart-header">
                    <img src="${item.image}" alt="${item.name}" class="cart-img" />
                    <div class="cart-info">
                        <h4>${item.name}</h4>
                        <p>₱${item.price.toFixed(2)} × ${item.quantity}</p>
                    </div>
                    <div class="quantity-controls">
                        <button class="qty-btn decrease" data-index="${index}">−</button>
                        <button class="qty-btn increase" data-index="${index}">+</button>
                        <button class="remove-btn" data-index="${index}">
                            <i class="bi bi-trash-fill"></i>
                        </button>
                    </div>
                </div>

                <div class="cart-special" data-index="${index}">
                    ${item.showInstruction
                    ? `<textarea class="special-input" rows="2" placeholder="Add your special request...">${item.special}</textarea>
                           <button class="save-special" data-index="${index}">Save</button>`
                    : `<p class="toggle-special" data-index="${index}">
                            <i>${item.special ? "Edit special instruction" : "Add special instruction"}</i>
                           </p>`}
                </div>
            `;

            cartContainer.appendChild(itemDiv);
        });

        itemCount.textContent = `${cart.length} Item${cart.length > 1 ? "s" : ""}`;
        totalDisplay.textContent = `TOTAL: ₱${total.toFixed(2)}`;
        attachCartEvents();
    }

    // 🟠 Cart Events
    function attachCartEvents() {
        // Remove item
        document.querySelectorAll(".remove-btn").forEach(btn => {
            btn.addEventListener("click", () => {
                const index = btn.dataset.index;
                cart.splice(index, 1);
                renderCart();
            });
        });

        // Increase / Decrease quantity
        document.querySelectorAll(".increase").forEach(btn => {
            btn.addEventListener("click", () => {
                const index = btn.dataset.index;
                cart[index].quantity++;
                renderCart();
            });
        });

        document.querySelectorAll(".decrease").forEach(btn => {
            btn.addEventListener("click", () => {
                const index = btn.dataset.index;
                if (cart[index].quantity > 1) {
                    cart[index].quantity--;
                } else {
                    cart.splice(index, 1);
                }
                renderCart();
            });
        });

        // Toggle special instruction
        document.querySelectorAll(".toggle-special").forEach(text => {
            text.addEventListener("click", () => {
                const index = text.dataset.index;
                cart[index].showInstruction = true;
                renderCart();
            });
        });

        // Save special instruction
        document.querySelectorAll(".save-special").forEach(btn => {
            btn.addEventListener("click", () => {
                const index = btn.dataset.index;
                const input = btn.parentElement.querySelector(".special-input");
                cart[index].special = input.value.trim();
                cart[index].showInstruction = false;
                renderCart();
            });
        });
    }
});
