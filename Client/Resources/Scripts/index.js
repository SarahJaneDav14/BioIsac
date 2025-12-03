const API_BASE = 'http://localhost:5266/api';
let authToken = localStorage.getItem('authToken');

// App state
let currentView = 'login';
let contacts = [];
let categories = [];

// Initialize app
document.addEventListener('DOMContentLoaded', () => {
    if (authToken) {
        verifyToken();
    } else {
        showLogin();
    }
});

async function verifyToken() {
    try {
        const response = await fetch(`${API_BASE}/auth/verify`, {
            headers: { 'Authorization': `Bearer ${authToken}` }
        });
        if (response.ok) {
            showDashboard();
        } else {
            localStorage.removeItem('authToken');
            authToken = null;
            showLogin();
        }
    } catch (error) {
        localStorage.removeItem('authToken');
        authToken = null;
        showLogin();
    }
}

function showLogin() {
    currentView = 'login';
    const app = document.getElementById('app');
    app.innerHTML = `
        <div class="login-container">
            <div class="container">
                <div class="row justify-content-center">
                    <div class="col-md-5">
                        <div class="card shadow login-card">
                            <div class="card-body p-5">
                                <h2 class="card-title text-center mb-4" style="color: #00d084; font-size: 2rem;">🔐 Admin Portal</h2>
                                <p class="text-center text-muted mb-4">Welcome back! Please sign in to continue.</p>
                            <form id="loginForm">
                                <div class="mb-3">
                                    <label for="username" class="form-label">Username</label>
                                    <input type="text" class="form-control" id="username" required>
                                </div>
                                <div class="mb-3">
                                    <label for="password" class="form-label">Password</label>
                                    <input type="password" class="form-control" id="password" required>
                                </div>
                                <div id="twoFactorSection" class="mb-3" style="display: none;">
                                    <label for="twoFactorCode" class="form-label">2FA Code</label>
                                    <input type="text" class="form-control" id="twoFactorCode" placeholder="Enter any code (for demonstration)" maxlength="6">
                                    <div id="qrCodeContainer" class="mt-3 text-center"></div>
                                    <small class="text-muted">2FA is for demonstration purposes - any code will work</small>
                                </div>
                                <div id="errorMessage" class="alert alert-danger" style="display: none;"></div>
                                <button type="submit" class="btn btn-primary w-100">Login</button>
                            </form>
                        </div>
                    </div>
                </div>
            </div>
        </div>
    `;

    document.getElementById('loginForm').addEventListener('submit', handleLogin);
}

async function handleLogin(e) {
    e.preventDefault();
    const username = document.getElementById('username').value;
    const password = document.getElementById('password').value;
    // 2FA is for show - accept any value or use placeholder
    const twoFactorCode = document.getElementById('twoFactorCode')?.value || '123456';

    try {
        const response = await fetch(`${API_BASE}/auth/login`, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ username, password, twoFactorCode })
        });

        const data = await response.json();

        if (response.ok) {
            if (data.requiresTwoFactor && !twoFactorCode) {
                document.getElementById('twoFactorSection').style.display = 'block';
                if (data.qrCodeUrl) {
                    document.getElementById('qrCodeContainer').innerHTML = 
                        `<img src="${data.qrCodeUrl}" alt="QR Code" class="img-fluid">`;
                }
                document.getElementById('errorMessage').style.display = 'none';
            } else if (data.token) {
                authToken = data.token;
                localStorage.setItem('authToken', authToken);
                showDashboard();
            }
        } else {
            // If 2FA is required but code was invalid, still show the field
            if (data.message && data.message.includes('2FA')) {
                document.getElementById('twoFactorSection').style.display = 'block';
            }
            document.getElementById('errorMessage').textContent = data.message || 'Login failed';
            document.getElementById('errorMessage').style.display = 'block';
        }
    } catch (error) {
        document.getElementById('errorMessage').textContent = 'Connection error. Make sure API is running.';
        document.getElementById('errorMessage').style.display = 'block';
    }
}

async function showDashboard() {
    currentView = 'dashboard';
    await loadContacts();
    await loadCategories();
    renderDashboard();
}

async function loadContacts() {
    try {
        const response = await fetch(`${API_BASE}/contacts`, {
            headers: { 'Authorization': `Bearer ${authToken}` }
        });
        if (response.ok) {
            contacts = await response.json();
        }
    } catch (error) {
        console.error('Failed to load contacts:', error);
    }
}

async function loadCategories() {
    try {
        const response = await fetch(`${API_BASE}/contacts/categories`, {
            headers: { 'Authorization': `Bearer ${authToken}` }
        });
        if (response.ok) {
            categories = await response.json();
        }
    } catch (error) {
        console.error('Failed to load categories:', error);
    }
}

function renderDashboard() {
    const app = document.getElementById('app');
    app.innerHTML = `
        <nav class="navbar navbar-dark">
            <div class="container-fluid">
                <span class="navbar-brand">🚀 BioIsac Admin Portal</span>
                <button class="btn btn-outline-light" onclick="logout()">Logout</button>
            </div>
        </nav>
        <div class="container mt-4" style="padding-bottom: 3rem;">
            <ul class="nav nav-tabs mb-4">
                <li class="nav-item">
                    <a class="nav-link ${currentView === 'dashboard' ? 'active' : ''}" href="#" onclick="showTab('contacts')">Contacts</a>
                </li>
                <li class="nav-item">
                    <a class="nav-link" href="#" onclick="showTab('email')">Send Email</a>
                </li>
            </ul>
            <div id="tabContent"></div>
        </div>
    `;
    showTab('contacts');
}

function showTab(tab) {
    const tabContent = document.getElementById('tabContent');
    if (tab === 'contacts') {
        tabContent.innerHTML = `
            <div class="card">
                <div class="card-header d-flex justify-content-between align-items-center">
                    <h5 class="mb-0">👥 Manage Contacts</h5>
                    <button class="btn btn-primary btn-sm" onclick="showAddContactForm()">+ Add Contact</button>
                </div>
                <div class="card-body">
                    <div id="contactsList"></div>
                </div>
            </div>
        `;
        renderContactsList();
    } else if (tab === 'email') {
        tabContent.innerHTML = `
            <div class="card">
                <div class="card-header">
                    <h5 class="mb-0">📧 Send Email</h5>
                </div>
                <div class="card-body">
                    <form id="emailForm" onsubmit="handleSendEmail(event)">
                        <div class="mb-3">
                            <label class="form-label">Send To</label>
                            <select class="form-select" id="emailRecipientType" onchange="updateRecipientOptions()">
                                <option value="category">Work Field</option>
                                <option value="person">Specific Person</option>
                            </select>
                        </div>
                        <div class="mb-3" id="recipientOptions"></div>
                        <div class="mb-3">
                            <label for="emailSubject" class="form-label">Subject</label>
                            <input type="text" class="form-control" id="emailSubject" required>
                        </div>
                        <div class="mb-3">
                            <label for="emailBody" class="form-label">Body (HTML supported)</label>
                            <textarea class="form-control" id="emailBody" rows="10" required></textarea>
                        </div>
                        <div id="emailMessage" class="alert" style="display: none;"></div>
                        <button type="submit" class="btn btn-primary">Send Email</button>
                    </form>
                </div>
            </div>
        `;
        updateRecipientOptions();
    }
}

function renderContactsList() {
    const contactsList = document.getElementById('contactsList');
    if (contacts.length === 0) {
        contactsList.innerHTML = '<p class="text-muted">No contacts yet. Add one to get started.</p>';
        return;
    }

    const grouped = {};
    contacts.forEach(contact => {
        if (!grouped[contact.workField]) {
            grouped[contact.workField] = [];
        }
        grouped[contact.workField].push(contact);
    });

    let html = '';
    for (const [category, categoryContacts] of Object.entries(grouped)) {
        html += `
            <div class="mb-4">
                <h6 class="text-primary" style="font-size: 1.2rem; margin-bottom: 1rem;">📁 ${category}</h6>
                <div class="table-responsive">
                    <table class="table table-striped">
                        <thead>
                            <tr>
                                <th>Name</th>
                                <th>Email</th>
                                <th>Actions</th>
                            </tr>
                        </thead>
                        <tbody>
                            ${categoryContacts.map(contact => `
                                <tr>
                                    <td>${contact.name}</td>
                                    <td>${contact.email}</td>
                                    <td>
                                        <button class="btn btn-sm btn-warning" onclick="editContact(${contact.id})">Edit</button>
                                        <button class="btn btn-sm btn-danger" onclick="deleteContact(${contact.id})">Delete</button>
                                    </td>
                                </tr>
                            `).join('')}
                        </tbody>
                    </table>
                </div>
            </div>
        `;
    }
    contactsList.innerHTML = html;
}

function showAddContactForm() {
    const modal = new bootstrap.Modal(document.createElement('div'));
    const modalElement = document.createElement('div');
    modalElement.className = 'modal fade';
    modalElement.innerHTML = `
        <div class="modal-dialog">
            <div class="modal-content">
                <div class="modal-header">
                    <h5 class="modal-title">➕ Add Contact</h5>
                    <button type="button" class="btn-close" data-bs-dismiss="modal"></button>
                </div>
                <div class="modal-body">
                    <form id="addContactForm">
                        <div class="mb-3">
                            <label class="form-label">Name</label>
                            <input type="text" class="form-control" id="contactName" required>
                        </div>
                        <div class="mb-3">
                            <label class="form-label">Email</label>
                            <input type="email" class="form-control" id="contactEmail" required>
                        </div>
                        <div class="mb-3">
                            <label class="form-label">Work Field</label>
                            <input type="text" class="form-control" id="contactWorkField" required>
                        </div>
                        <div id="contactMessage" class="alert" style="display: none;"></div>
                    </form>
                </div>
                <div class="modal-footer">
                    <button type="button" class="btn btn-secondary" data-bs-dismiss="modal">Cancel</button>
                    <button type="button" class="btn btn-primary" onclick="saveContact()">Save</button>
                </div>
            </div>
        </div>
    `;
    document.body.appendChild(modalElement);
    const bsModal = new bootstrap.Modal(modalElement);
    bsModal.show();
    modalElement.addEventListener('hidden.bs.modal', () => modalElement.remove());
}

async function saveContact() {
    const name = document.getElementById('contactName').value;
    const email = document.getElementById('contactEmail').value;
    const workField = document.getElementById('contactWorkField').value;

    try {
        const response = await fetch(`${API_BASE}/contacts`, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                'Authorization': `Bearer ${authToken}`
            },
            body: JSON.stringify({ name, email, workField })
        });

        const data = await response.json();
        const messageDiv = document.getElementById('contactMessage');

        if (response.ok) {
            messageDiv.className = 'alert alert-success';
            messageDiv.textContent = 'Contact added successfully';
            messageDiv.style.display = 'block';
            await loadContacts();
            setTimeout(() => {
                bootstrap.Modal.getInstance(document.querySelector('.modal')).hide();
                renderContactsList();
            }, 1000);
        } else {
            messageDiv.className = 'alert alert-danger';
            messageDiv.textContent = data.message || 'Failed to add contact';
            messageDiv.style.display = 'block';
        }
    } catch (error) {
        const messageDiv = document.getElementById('contactMessage');
        messageDiv.className = 'alert alert-danger';
        messageDiv.textContent = 'Connection error';
        messageDiv.style.display = 'block';
    }
}

async function editContact(id) {
    const contact = contacts.find(c => c.id === id);
    if (!contact) return;

    const modal = new bootstrap.Modal(document.createElement('div'));
    const modalElement = document.createElement('div');
    modalElement.className = 'modal fade';
    modalElement.innerHTML = `
        <div class="modal-dialog">
            <div class="modal-content">
                <div class="modal-header">
                    <h5 class="modal-title">✏️ Edit Contact</h5>
                    <button type="button" class="btn-close" data-bs-dismiss="modal"></button>
                </div>
                <div class="modal-body">
                    <form id="editContactForm">
                        <div class="mb-3">
                            <label class="form-label">Name</label>
                            <input type="text" class="form-control" id="editContactName" value="${contact.name}" required>
                        </div>
                        <div class="mb-3">
                            <label class="form-label">Email</label>
                            <input type="email" class="form-control" id="editContactEmail" value="${contact.email}" required>
                        </div>
                        <div class="mb-3">
                            <label class="form-label">Work Field</label>
                            <input type="text" class="form-control" id="editContactWorkField" value="${contact.workField}" required>
                        </div>
                        <div id="editContactMessage" class="alert" style="display: none;"></div>
                    </form>
                </div>
                <div class="modal-footer">
                    <button type="button" class="btn btn-secondary" data-bs-dismiss="modal">Cancel</button>
                    <button type="button" class="btn btn-primary" onclick="updateContact(${id})">Update</button>
                </div>
            </div>
        </div>
    `;
    document.body.appendChild(modalElement);
    const bsModal = new bootstrap.Modal(modalElement);
    bsModal.show();
    modalElement.addEventListener('hidden.bs.modal', () => modalElement.remove());
}

async function updateContact(id) {
    const name = document.getElementById('editContactName').value;
    const email = document.getElementById('editContactEmail').value;
    const workField = document.getElementById('editContactWorkField').value;

    try {
        const response = await fetch(`${API_BASE}/contacts/${id}`, {
            method: 'PUT',
            headers: {
                'Content-Type': 'application/json',
                'Authorization': `Bearer ${authToken}`
            },
            body: JSON.stringify({ name, email, workField })
        });

        const data = await response.json();
        const messageDiv = document.getElementById('editContactMessage');

        if (response.ok) {
            messageDiv.className = 'alert alert-success';
            messageDiv.textContent = 'Contact updated successfully';
            messageDiv.style.display = 'block';
            await loadContacts();
            setTimeout(() => {
                bootstrap.Modal.getInstance(document.querySelector('.modal')).hide();
                renderContactsList();
            }, 1000);
        } else {
            messageDiv.className = 'alert alert-danger';
            messageDiv.textContent = data.message || 'Failed to update contact';
            messageDiv.style.display = 'block';
        }
    } catch (error) {
        const messageDiv = document.getElementById('editContactMessage');
        messageDiv.className = 'alert alert-danger';
        messageDiv.textContent = 'Connection error';
        messageDiv.style.display = 'block';
    }
}

async function deleteContact(id) {
    if (!confirm('Are you sure you want to delete this contact?')) return;

    try {
        const response = await fetch(`${API_BASE}/contacts/${id}`, {
            method: 'DELETE',
            headers: { 'Authorization': `Bearer ${authToken}` }
        });

        if (response.ok) {
            await loadContacts();
            renderContactsList();
        } else {
            alert('Failed to delete contact');
        }
    } catch (error) {
        alert('Connection error');
    }
}

function updateRecipientOptions() {
    const type = document.getElementById('emailRecipientType').value;
    const optionsDiv = document.getElementById('recipientOptions');

    if (type === 'category') {
        optionsDiv.innerHTML = `
            <label class="form-label">Select Work Field</label>
            <select class="form-select" id="emailCategory" required>
                <option value="">-- Select Work Field --</option>
                ${categories.map(cat => `<option value="${cat}">${cat}</option>`).join('')}
            </select>
        `;
    } else {
        optionsDiv.innerHTML = `
            <label class="form-label">Select Person</label>
            <select class="form-select" id="emailPerson" required>
                <option value="">-- Select Person --</option>
                ${contacts.map(c => `<option value="${c.id}">${c.name} (${c.email})</option>`).join('')}
            </select>
        `;
    }
}

async function handleSendEmail(e) {
    e.preventDefault();
    const type = document.getElementById('emailRecipientType').value;
    const subject = document.getElementById('emailSubject').value;
    const body = document.getElementById('emailBody').value;
    const messageDiv = document.getElementById('emailMessage');

    let request = { subject, body };

    if (type === 'category') {
        const category = document.getElementById('emailCategory').value;
        if (!category) {
            messageDiv.className = 'alert alert-danger';
            messageDiv.textContent = 'Please select a work field';
            messageDiv.style.display = 'block';
            return;
        }
        request.category = category;
    } else {
        const personId = document.getElementById('emailPerson').value;
        if (!personId) {
            messageDiv.className = 'alert alert-danger';
            messageDiv.textContent = 'Please select a person';
            messageDiv.style.display = 'block';
            return;
        }
        request.contactId = parseInt(personId);
    }

    try {
        const response = await fetch(`${API_BASE}/email/send`, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                'Authorization': `Bearer ${authToken}`
            },
            body: JSON.stringify(request)
        });

        const data = await response.json();

        if (response.ok) {
            messageDiv.className = 'alert alert-success';
            messageDiv.textContent = 'Email sent successfully!';
            messageDiv.style.display = 'block';
            document.getElementById('emailForm').reset();
        } else {
            messageDiv.className = 'alert alert-danger';
            messageDiv.textContent = data.message || 'Failed to send email';
            messageDiv.style.display = 'block';
        }
    } catch (error) {
        messageDiv.className = 'alert alert-danger';
        messageDiv.textContent = 'Connection error. Make sure email is configured.';
        messageDiv.style.display = 'block';
    }
}

function logout() {
    localStorage.removeItem('authToken');
    authToken = null;
    showLogin();
}

