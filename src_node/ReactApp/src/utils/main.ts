// @ts-nocheck


// API Configuration
const API_BASE_URL = 'https://redemptionfe.azurewebsites.net/api';
let membersData = [];
let householdsData = [];
let usersData = [];
let authToken = null;
let currentAttendanceData = [];
let selectedAttendanceMembers = new Set();

// API Service Functions
class MemberService {
  static getAuthHeaders() {
    const token = localStorage.getItem('authToken');
    console.log('🔐 Auth Token:', token ? 'Present' : 'Missing');
    return {
      'Authorization': token ? `Bearer ${token}` : '',
      'Content-Type': 'application/json'
    };
  }

  static async loadMembers() {
    try {
      console.log('📡 Loading members from API...');

      // Debug: Check if token exists
      const token = localStorage.getItem('authToken');
      console.log('🔐 Auth Token Present:', !!token);
      if (token) {
        console.log('🔐 Token Length:', token.length);
        // Don't log the actual token for security
      }
      const response = await fetch('https://redemptionfe.azurewebsites.net/api/members', {
        headers: this.getAuthHeaders()
      });

      console.log('📡 API Response Status:', response.status);

      if (!response.ok) {
        throw new Error(`HTTP error! status: ${response.status}`);
      }

      const result = await response.json();
      console.log('✅ Raw API Response:', result);

      // Handle different possible response structures
      let membersArray = [];

      if (Array.isArray(result)) {
        membersArray = result;
      } else if (result.members && Array.isArray(result.members)) {
        membersArray = result.members;
      } else if (result.data && Array.isArray(result.data)) {
        membersArray = result.data;
      } else {
        console.warn('⚠️ Unexpected API response format:', result);
        membersArray = [];
      }

      console.log('✅ Members loaded successfully:', membersArray.length, 'members');

      // In the map function where you process members, add more safety:
      const processedMembers = membersArray.map(member => {
        try {
          const firstName = member.firstName || member.FirstName || '';
          const lastName = member.lastName || member.LastName || '';

          // Always generate FullName from FirstName + LastName
          const generatedFullName = `${firstName} ${lastName}`.trim();

          return {
            ...member,
            Id: member.id || member.Id,
            FirstName: firstName,
            LastName: lastName,
            FullName: generatedFullName || 'Unknown Member',
            Gender: member.gender || member.Gender || 'Unknown',
            PhoneNumber: member.phoneNumber || member.PhoneNumber || member.phone || '',
            Email: member.email || member.Email || '',
            DateOfBirth: member.dateOfBirth || member.DateOfBirth,
            HouseholdName: member.householdName || member.HouseholdName || 'No Household',
            ImmigrationStatus: member.immigrationStatus || member.ImmigrationStatus || 'Unknown',
            IsFlagged: member.isFlagged || member.IsFlagged || false,
            DateJoined: member.dateJoined || member.DateJoined,
          };
        } catch (error) {
          console.error('Error processing member:', member, error);
          // Return a safe fallback member object
          return {
            Id: member?.id || member?.Id || Date.now(),
            FirstName: 'Error',
            LastName: 'Processing',
            FullName: 'Error Processing Member',
            Gender: 'Unknown',
            PhoneNumber: '',
            Email: '',
            DateOfBirth: null,
            HouseholdName: 'No Household',
            ImmigrationStatus: 'Unknown',
            IsFlagged: false,
            DateJoined: null
          };
        }
      });

      membersData = processedMembers;
      return processedMembers;
    } catch (error) {
      console.error('❌ Failed to load members:', error);
      membersData = [];
      return [];
    }
  }
  static async addMember(memberData) {
    try {
      console.log('📝 Adding new member:', memberData);

      // Format dates properly
      let formattedDateOfBirth = null;
      if (memberData.dateOfBirth) {
        const dateObj = new Date(memberData.dateOfBirth);
        if (!isNaN(dateObj.getTime())) {
          formattedDateOfBirth = dateObj.toISOString();
        }
      }

      // Ensure required fields are present
      const payload = {
        FirstName: memberData.firstName || '',
        LastName: memberData.lastName || '',
        FullName: memberData.fullName || `${memberData.firstName} ${memberData.lastName}`,
        Gender: memberData.gender || 'Unknown',
        PhoneNumber: memberData.phone || null,
        Email: memberData.email || null,
        DateOfBirth: formattedDateOfBirth,
        ImmigrationStatus: memberData.immigrationStatus || null
      };

      console.log('📦 Sending payload to API:', JSON.stringify(payload, null, 2));

      const response = await fetch(`${API_BASE_URL}/Members`, {
        method: 'POST',
        headers: this.getAuthHeaders(),
        body: JSON.stringify(payload)
      });

      console.log('📡 Add Member Response Status:', response.status);

      if (!response.ok) {
        const errorText = await response.text();
        console.error('❌ Add Member Error Response:', errorText);
        throw new Error(`Failed to add member: ${errorText}`);
      }

      const result = await response.json();
      console.log('✅ Member added successfully:', result);

      // Refresh members data immediately after adding
      await this.loadMembers();
      return result;
    } catch (error) {
      console.error('❌ Failed to add member:', error);
      throw error;
    }
  }

  static async uploadCSV(file) {
    try {
      const formData = new FormData();
      formData.append('file', file);

      const token = localStorage.getItem('authToken');
      const response = await fetch(`${API_BASE_URL}/Members/upload`, {
        method: 'POST',
        headers: {
          'Authorization': token ? `Bearer ${token}` : '',
        },
        body: formData
      });

      console.log('📤 Upload Response Status:', response.status);

      if (response.status === 401) {
        throw new Error('Authentication failed. Please login again.');
      }

      if (!response.ok) {
        const errorText = await response.text();
        console.error('❌ Upload Error:', errorText);
        throw new Error(`Upload failed: ${errorText}`);
      }

      const result = await response.json();
      console.log('✅ Upload successful:', result);

      // Reload members after successful upload
      await this.loadMembers();
      return result;
    } catch (error) {
      console.error('❌ Failed to upload CSV:', error);
      throw error;
    }
  }
}

// Household Service Functions
class HouseholdService {
  static getAuthHeaders() {
    const token = localStorage.getItem('authToken');
    return {
      'Authorization': token ? `Bearer ${token}` : '',
      'Content-Type': 'application/json'
    };
  }

  static async loadHouseholds() {
    try {
      const response = await fetch(`${API_BASE_URL}/Households`, {
        method: 'GET',
        headers: this.getAuthHeaders()
      });

      if (response.status === 401) {
        throw new Error('Authentication failed. Please login again.');
      }

      if (!response.ok) {
        throw new Error(`HTTP error! status: ${response.status}`);
      }

      householdsData = await response.json();
      return householdsData;
    } catch (error) {
      console.error('Failed to load households:', error);
      throw error;
    }
  }

  static async addHousehold(householdData) {
    try {
      const response = await fetch(`${API_BASE_URL}/Households`, {
        method: 'POST',
        headers: this.getAuthHeaders(),
        body: JSON.stringify(householdData)
      });

      if (response.status === 401) {
        throw new Error('Authentication failed. Please login again.');
      }

      if (!response.ok) {
        throw new Error(`HTTP error! status: ${response.status}`);
      }

      const newHousehold = await response.json();
      householdsData.push(newHousehold);
      return newHousehold;
    } catch (error) {
      console.error('Failed to add household:', error);
      throw error;
    }
  }
}

// Attendance Service Functions
class AttendanceService {
  static getAuthHeaders() {
    const token = localStorage.getItem('authToken');
    return {
      'Authorization': token ? `Bearer ${token}` : '',
      'Content-Type': 'application/json'
    };
  }

  static async loadAttendance(serviceDate, page = 1, limit = 10) {
    try {
      const params = new URLSearchParams({
        date: serviceDate,
        page: page.toString(),
        limit: limit.toString()
      });

      const response = await fetch(`${API_BASE_URL}/Attendance?${params}`, {
        method: 'GET',
        headers: this.getAuthHeaders()
      });

      if (response.status === 401) {
        throw new Error('Authentication failed. Please login again.');
      }

      if (!response.ok) {
        throw new Error(`HTTP error! status: ${response.status}`);
      }

      const data = await response.json();
      currentAttendanceData = data.attendance || [];
      return data;
    } catch (error) {
      console.error('Failed to load attendance:', error);
      throw error;
    }
  }

  static async saveBulkAttendance(attendanceData) {
    try {
      const response = await fetch(`${API_BASE_URL}/Attendance/bulk`, {
        method: 'POST',
        headers: this.getAuthHeaders(),
        body: JSON.stringify(attendanceData)
      });

      if (response.status === 401) {
        throw new Error('Authentication failed. Please login again.');
      }

      if (!response.ok) {
        throw new Error(`HTTP error! status: ${response.status}`);
      }

      const result = await response.json();
      return result;
    } catch (error) {
      console.error('Failed to save attendance:', error);
      throw error;
    }
  }
}

// User Service Functions
class UserService {
  static getAuthHeaders() {
    const token = localStorage.getItem('authToken');
    return {
      'Authorization': token ? `Bearer ${token}` : '',
      'Content-Type': 'application/json'
    };
  }

  static async loadUsers() {
    try {
      const response = await fetch(`${API_BASE_URL}/Users`, {
        method: 'GET',
        headers: this.getAuthHeaders()
      });

      if (response.status === 401) {
        throw new Error('Authentication failed. Please login again.');
      }

      if (response.status === 403) {
        throw new Error('Access denied. Admin privileges required.');
      }

      if (!response.ok) {
        throw new Error(`HTTP error! status: ${response.status}`);
      }

      usersData = await response.json();
      return usersData;
    } catch (error) {
      console.error('Failed to load users:', error);
      throw error;
    }
  }

  static async addUser(userData) {
    try {
      const token = localStorage.getItem('authToken');
      console.log('🔐 Auth Token Present:', !!token);

      const response = await fetch(`${API_BASE_URL}/Users`, {
        method: 'POST',
        headers: {
          'Authorization': token ? `Bearer ${token}` : '',
          'Content-Type': 'application/json'
        },
        body: JSON.stringify(userData)
      });

      console.log('👤 Create User Response Status:', response.status);

      if (response.status === 401) {
        throw new Error('Authentication failed. Please login again.');
      }

      if (response.status === 403) {
        throw new Error('Access denied. Admin privileges required.');
      }

      if (!response.ok) {
        const errorText = await response.text();
        console.error('❌ Create User Error:', errorText);
        throw new Error(`Failed to create user: ${errorText}`);
      }

      const newUser = await response.json();
      console.log('✅ User created successfully:', newUser);
      return newUser;
    } catch (error) {
      console.error('❌ Failed to create user:', error);
      throw error;
    }
  }
}

// Dashboard Service Functions - FIXED: Added error handling for missing endpoint
class DashboardService {
  static getAuthHeaders() {
    const token = localStorage.getItem('authToken');
    return {
      'Authorization': token ? `Bearer ${token}` : '',
      'Content-Type': 'application/json'
    };
  }

  static async loadDashboardStats() {
    try {
      console.log('📊 Loading dashboard stats...');
      const response = await fetch(`${API_BASE_URL}/Dashboard/stats`, {
        method: 'GET',
        headers: this.getAuthHeaders()
      });

      // If dashboard endpoint doesn't exist, use fallback data
      if (response.status === 404 || response.status === 500) {
        console.warn('⚠️ Dashboard endpoint not available, using fallback data');
        return this.getFallbackStats();
      }

      if (!response.ok) {
        console.warn('⚠️ Dashboard stats not available, using fallback data');
        return this.getFallbackStats();
      }

      const stats = await response.json();
      console.log('✅ Dashboard stats loaded:', stats);
      return stats;
    } catch (error) {
      console.warn('⚠️ Failed to load dashboard stats, using fallback:', error.message);
      return this.getFallbackStats();
    }
  }

  static getFallbackStats() {
    // Calculate stats from members data as fallback
    const totalMembers = membersData.length;
    const flaggedMembers = membersData.filter(m => m.IsFlagged).length;

    // Simple calculation for demo purposes
    return {
      totalMembers: totalMembers,
      attendanceRate: 75, // Default value
      flaggedMembers: flaggedMembers,
      expiringDocuments: membersData.filter(m =>
        m.ImmigrationStatus && m.ImmigrationStatus.includes('Expires')
      ).length
    };
  }

  static async loadBirthdays() {
    try {
      // Fallback to local calculation if API not available
      const today = new Date();
      const next30Days = new Date(today);
      next30Days.setDate(today.getDate() + 30);

      return membersData.filter(member => {
        if (!member.DateOfBirth) return false;
        try {
          const birthDate = new Date(member.DateOfBirth);
          const currentYearBirthday = new Date(today.getFullYear(), birthDate.getMonth(), birthDate.getDate());

          return currentYearBirthday >= today && currentYearBirthday <= next30Days;
        } catch (e) {
          return false;
        }
      }).sort((a, b) => {
        try {
          const aDate = new Date(a.DateOfBirth);
          const bDate = new Date(b.DateOfBirth);
          return aDate - bDate;
        } catch (e) {
          return 0;
        }
      });
    } catch (error) {
      console.error('Failed to load birthdays:', error);
      return [];
    }
  }
}

// Utility function to initialize app data
async function initializeAppData() {
  try {
    await Promise.all([
      MemberService.loadMembers().catch(error => {
        console.error('Failed to load members:', error);
        return [];
      }),
      HouseholdService.loadHouseholds().catch(error => {
        console.error('Failed to load households:', error);
        return [];
      }),
      UserService.loadUsers().catch(error => {
        console.error('Failed to load users:', error);
        return [];
      })
    ]);
    console.log('✅ App data loaded successfully');
  } catch (error) {
    console.error('Failed to initialize app data:', error);
    // Don't throw error to prevent app from crashing
  }
}

// DOM Elements
const loginPage = document.getElementById('login-page');
const app = document.getElementById('app');
const loginButton = document.getElementById('login-button');
const logoutButton = document.getElementById('logout-button');
const userMenuButton = document.getElementById('user-menu-button');
const sidebarToggle = document.getElementById('sidebar-toggle');
const sidebar = document.querySelector('.sidebar');
const memberModal = document.getElementById('member-modal');
const addMemberBtn = document.getElementById('add-member-btn');
const addMemberFloatingBtn = document.getElementById('add-member-floating-btn');
const closeMemberModal = document.getElementById('close-member-modal');
const cancelMember = document.getElementById('cancel-member');
const saveMember = document.getElementById('save-member');
const householdModal = document.getElementById('household-modal');
const addHouseholdBtn = document.getElementById('add-household-btn');
const addHouseholdFloatingBtn = document.getElementById('add-household-floating-btn');
const closeHouseholdModal = document.getElementById('close-household-modal');
const cancelHousehold = document.getElementById('cancel-household');
const saveHousehold = document.getElementById('save-household');
const userModal = document.getElementById('user-modal');
const addUserBtn = document.getElementById('add-user-btn');
const closeUserModal = document.getElementById('close-user-modal');
const cancelUser = document.getElementById('cancel-user');
const saveUser = document.getElementById('save-user');
const householdDetailsModal = document.getElementById('household-details-modal');
const closeHouseholdDetailsModal = document.getElementById('close-household-details-modal');
const pages = document.querySelectorAll('.page');
const sidebarLinks = document.querySelectorAll('.sidebar-link');
const autosaveIndicator = document.getElementById('autosave-indicator');

// Pagination and data management
let currentMembersPage = 1;
let currentHouseholdsPage = 1;
let currentAttendancePage = 1;
let membersPerPage = 10;
let householdsPerPage = 9;
let attendancePerPage = 10;
let filteredMembers = [];
let filteredHouseholds = [];
let chartInstances = {};

// Page Navigation
async function showPage(pageId) {
  pages.forEach(page => {
    page.classList.add('hidden');
  });

  const pageToShow = document.getElementById(`${pageId}-page`);
  if (pageToShow) {
    pageToShow.classList.remove('hidden');

    // Initialize page-specific functionality
    if (pageId === 'members') {
      await initializeMembersPage();
    } else if (pageId === 'households') {
      await initializeHouseholdsPage();
    } else if (pageId === 'attendance') {
      await initializeAttendancePage();
    } else if (pageId === 'reports') {
      await initializeReportsPage();
    } else if (pageId === 'dashboard') {
      await initializeDashboardPage();
    } else if (pageId === 'settings') {
      await initializeSettingsPage();
    }
  } else {
    document.getElementById('dashboard-page').classList.remove('hidden');
  }
}

// Authentication
loginButton.addEventListener('click', async () => {
  const email = document.getElementById('email').value;
  const password = document.getElementById('password').value;

  // Simple validation
  if (!email || !password) {
    alert('Please enter both email and password');
    return;
  }

  try {
    // Show loading state
    loginButton.disabled = true;
    loginButton.innerHTML = '<i class="fas fa-spinner fa-spin mr-2"></i>Logging in...';

    // Call your actual authentication endpoint
    const response = await fetch(`${API_BASE_URL}/Auth/login`, {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
      },
      body: JSON.stringify({ email, password })
    });

    if (!response.ok) {
      const errorData = await response.json().catch(() => ({ message: 'Login failed' }));
      throw new Error(errorData.message || `Login failed with status: ${response.status}`);
    }

    const authData = await response.json();

    if (!authData.token) {
      throw new Error('No authentication token received');
    }

    // Store the real token from backend
    localStorage.setItem('authToken', authData.token);
    localStorage.setItem('user', JSON.stringify(authData.user || { name: 'User', email: email }));

    // Update UI
    const userNameElement = document.getElementById('user-name');
    if (userNameElement && authData.user) {
      userNameElement.textContent = authData.user.name;
    }

    loginPage.classList.add('hidden');
    app.classList.remove('hidden');

    // Initialize app data
    await initializeAppData();
    showPage('dashboard');

    // Reinitialize charts
    destroyCharts();
    initCharts();

  } catch (error) {
    console.error('Login failed:', error);
    alert(`Login failed: ${error.message}`);
  } finally {
    // Reset login button
    loginButton.disabled = false;
    loginButton.textContent = 'Login';
  }
});

logoutButton.addEventListener('click', () => {
  // Clear authentication data
  localStorage.removeItem('authToken');
  localStorage.removeItem('user');

  // Destroy charts
  destroyCharts();

  app.classList.add('hidden');
  loginPage.classList.remove('hidden');
});

// Check for existing authentication on page load
document.addEventListener('DOMContentLoaded', function () {
  const token = localStorage.getItem('authToken');
  const user = localStorage.getItem('user');

  if (token && user) {
    // User is already logged in
    loginPage.classList.add('hidden');
    app.classList.remove('hidden');

    const userData = JSON.parse(user);
    document.getElementById('user-name').textContent = userData.name;

    // Initialize app
    initializeAppData().then(() => {
      showPage('dashboard');
      initCharts();
    }).catch(error => {
      console.error('Failed to initialize app:', error);
      // If initialization fails, log out
      localStorage.removeItem('authToken');
      localStorage.removeItem('user');
      app.classList.add('hidden');
      loginPage.classList.remove('hidden');
    });
  }

  // Initialize date pickers
  if (document.getElementById('service-date')) {
    flatpickr("#service-date", {
      dateFormat: "M d, Y",
      defaultDate: "today"
    });
  }

  if (document.getElementById('report-start-date')) {
    flatpickr("#report-start-date", {
      dateFormat: "M d, Y",
      defaultDate: new Date().setMonth(new Date().getMonth() - 1)
    });
  }

  if (document.getElementById('report-end-date')) {
    flatpickr("#report-end-date", {
      dateFormat: "M d, Y",
      defaultDate: "today"
    });
  }

  if (document.getElementById('dob')) {
    flatpickr("#dob", {
      dateFormat: "M d, Y"
    });
  }

  if (document.getElementById('document-expiry')) {
    flatpickr("#document-expiry", {
      dateFormat: "M d, Y"
    });
  }

  if (document.getElementById('date-joined')) {
    flatpickr("#date-joined", {
      dateFormat: "M d, Y",
      defaultDate: "today"
    });
  }

  // Initialize auto-populate for full name
  const firstNameInput = document.getElementById('first-name');
  const lastNameInput = document.getElementById('last-name');
  const fullNameInput = document.getElementById('full-name');

  function updateFullName() {
    const firstName = firstNameInput?.value.trim() || '';
    const lastName = lastNameInput?.value.trim() || '';
    const fullName = `${firstName} ${lastName}`.trim();

    if (fullNameInput) {
      fullNameInput.value = fullName;
    }
  }

  if (firstNameInput && lastNameInput) {
    firstNameInput.addEventListener('input', updateFullName);
    lastNameInput.addEventListener('input', updateFullName);
  }
});

// User Menu Toggle
userMenuButton.addEventListener('click', () => {
  const dropdown = userMenuButton.closest('.dropdown');
  dropdown.classList.toggle('open');
});

// Close dropdowns when clicking outside
document.addEventListener('click', (e) => {
  if (!e.target.closest('#user-menu-button')) {
    const dropdown = document.querySelector('.dropdown');
    if (dropdown) {
      dropdown.classList.remove('open');
    }
  }
});

// Sidebar toggle
sidebarToggle.addEventListener('click', () => {
  sidebar.classList.toggle('collapsed');
});

// Navigation
sidebarLinks.forEach(link => {
  link.addEventListener('click', (e) => {
    e.preventDefault();
    const pageId = link.getAttribute('href').substring(1);
    showPage(pageId);

    if (window.innerWidth < 768) {
      sidebar.classList.add('collapsed');
    }
  });
});

// Modal Management
function openModal(modal) {
  modal.classList.remove('hidden');
  modal.classList.add('flex');
}

function closeModal(modal) {
  modal.classList.add('hidden');
  modal.classList.remove('flex');
}

// Member Modal
if (addMemberBtn) {
  addMemberBtn.addEventListener('click', () => openModal(memberModal));
}

if (addMemberFloatingBtn) {
  addMemberFloatingBtn.addEventListener('click', () => openModal(memberModal));
}

if (closeMemberModal) {
  closeMemberModal.addEventListener('click', () => closeModal(memberModal));
}

if (cancelMember) {
  cancelMember.addEventListener('click', () => closeModal(memberModal));
}

if (saveMember) {
  saveMember.addEventListener('click', async () => {
    try {
      // Get form data with first/last name
      const memberData = {
        firstName: document.getElementById('first-name').value,
        lastName: document.getElementById('last-name').value,
        fullName: document.getElementById('full-name').value,
        gender: document.querySelector('#member-modal select').value,
        phone: document.querySelector('#member-modal input[placeholder="Enter phone number"]').value,
        email: document.querySelector('#member-modal input[placeholder="Enter email address"]').value,
        dateOfBirth: document.getElementById('dob').value,
        immigrationStatus: document.querySelector('#member-modal select:nth-child(2)').value
      };

      // Validate required fields
      if (!memberData.firstName || !memberData.lastName || !memberData.gender) {
        alert('Please fill in all required fields (First Name, Last Name, Gender)');
        return;
      }

      // Send to API
      await MemberService.addMember(memberData);
      closeModal(memberModal);
      alert('Member saved successfully!');

      // Clear form
      document.getElementById('first-name').value = '';
      document.getElementById('last-name').value = '';
      document.getElementById('full-name').value = '';
      document.querySelector('#member-modal select').value = '';
      document.querySelector('#member-modal input[placeholder="Enter phone number"]').value = '';
      document.querySelector('#member-modal input[placeholder="Enter email address"]').value = '';
      document.getElementById('dob').value = '';
      document.querySelector('#member-modal select:nth-child(2)').value = '';

    } catch (error) {
      console.error('Error saving member:', error);
      alert(`Failed to save member: ${error.message}`);
    }
  });
}

// Household Modal
if (addHouseholdBtn) {
  addHouseholdBtn.addEventListener('click', () => openModal(householdModal));
}

if (addHouseholdFloatingBtn) {
  addHouseholdFloatingBtn.addEventListener('click', () => openModal(householdModal));
}

if (closeHouseholdModal) {
  closeHouseholdModal.addEventListener('click', () => closeModal(householdModal));
}

if (cancelHousehold) {
  cancelHousehold.addEventListener('click', () => closeModal(householdModal));
}

if (saveHousehold) {
  saveHousehold.addEventListener('click', async () => {
    try {
      // Get form data
      const householdData = {
        name: document.querySelector('#household-modal input[placeholder="e.g., Smith Family"]').value,
        head: document.querySelector('#household-modal select').value,
        address: document.querySelector('#household-modal input[placeholder="Enter household address"]').value,
        phone: document.querySelector('#household-modal input[placeholder="Enter phone number"]').value,
        email: document.querySelector('#household-modal input[placeholder="Enter email address"]').value
      };

      // Validate required fields
      if (!householdData.name || !householdData.head || !householdData.address || !householdData.phone) {
        alert('Please fill in all required fields (Name, Head of Household, Address, Phone)');
        return;
      }

      // Send to API
      await HouseholdService.addHousehold(householdData);
      closeModal(householdModal);
      alert('Household saved successfully!');
      // Refresh the households list
      await initializeHouseholdsPage();
    } catch (error) {
      console.error('Error saving household:', error);
      alert(`Failed to save household: ${error.message}`);
    }
  });
}

// User Modal
if (addUserBtn) {
  addUserBtn.addEventListener('click', () => openModal(userModal));
}

if (closeUserModal) {
  closeUserModal.addEventListener('click', () => closeModal(userModal));
}

if (cancelUser) {
  cancelUser.addEventListener('click', () => closeModal(userModal));
}

if (saveUser) {
  saveUser.addEventListener('click', async () => {
    const name = document.getElementById('user-name-input').value;
    const email = document.getElementById('user-email-input').value;
    const password = document.getElementById('user-password-input').value;
    const confirmPassword = document.getElementById('user-confirm-password').value;
    const role = document.getElementById('user-role-select').value;

    if (!name || !email || !password || !confirmPassword) {
      alert('Please fill in all required fields');
      return;
    }

    if (password !== confirmPassword) {
      alert('Passwords do not match');
      return;
    }

    if (password.length < 6) {
      alert('Password must be at least 6 characters long');
      return;
    }

    try {
      // Send to API
      await UserService.addUser({
        FullName: name,
        Email: email,
        Password: password,
        RoleLevel: parseInt(role)
      });
      closeModal(userModal);
      alert('User created successfully!');
      // Refresh the users list
      await initializeSettingsPage();

      // Reset form
      document.getElementById('user-name-input').value = '';
      document.getElementById('user-email-input').value = '';
      document.getElementById('user-password-input').value = '';
      document.getElementById('user-confirm-password').value = '';
    } catch (error) {
      console.error('Error creating user:', error);
      alert(`Failed to create user: ${error.message}`);
    }
  });
}

// Household Details Modal
if (closeHouseholdDetailsModal) {
  closeHouseholdDetailsModal.addEventListener('click', () => closeModal(householdDetailsModal));
}

// Initialize Dashboard Page
async function initializeDashboardPage() {
  try {
    // Load dashboard stats
    const stats = await DashboardService.loadDashboardStats();

    // Update dashboard stats
    document.getElementById('total-members').textContent = stats.totalMembers || membersData.length;
    document.getElementById('attendance-rate').textContent = `${stats.attendanceRate || 0}%`;
    document.getElementById('flagged-members').textContent = stats.flaggedMembers || 0;
    document.getElementById('expiring-documents').textContent = stats.expiringDocuments || 0;

    // Load and display birthdays
    const upcomingBirthdays = await DashboardService.loadBirthdays();
    const birthdaysTableBody = document.getElementById('birthdays-table-body');
    birthdaysTableBody.innerHTML = '';

    upcomingBirthdays.slice(0, 10).forEach(birthday => {
      try {
        const birthDate = new Date(birthday.DateOfBirth);
        const today = new Date();
        const nextBirthday = new Date(today.getFullYear(), birthDate.getMonth(), birthDate.getDate());
        const daysUntil = Math.ceil((nextBirthday - today) / (1000 * 60 * 60 * 24));

        const row = document.createElement('tr');
        row.className = 'border-b border-gray-100';
        row.innerHTML = `
              <td class="py-2 text-sm text-gray-800">${birthday.FullName}</td>
              <td class="py-2 text-sm text-gray-600">${birthDate.toLocaleDateString('en-US', { month: 'long', day: 'numeric' })}</td>
              <td class="py-2 text-sm text-gray-600">${daysUntil} days</td>
              <td class="py-2">
                <button class="text-indigo-600 hover:text-indigo-900 text-sm" onclick="sendBirthdayWish('${birthday.FullName}', '${birthday.Email}', '${birthday.PhoneNumber}')">
                  <i class="fas fa-envelope mr-1"></i> Send Wish
                </button>
              </td>
            `;
        birthdaysTableBody.appendChild(row);
      } catch (e) {
        console.warn('Skipping invalid birthday data:', birthday);
      }
    });

    // Update welfare dashboard
    document.getElementById('follow-up-count').textContent = stats.flaggedMembers || 0;
    document.getElementById('recent-followups').textContent = '0'; // This would come from a follow-ups API

    const welfareTableBody = document.getElementById('welfare-table-body');
    welfareTableBody.innerHTML = '';

    // Show flagged members in welfare dashboard
    const flaggedMembers = membersData.filter(m => m.IsFlagged).slice(0, 5);
    flaggedMembers.forEach(member => {
      const row = document.createElement('tr');
      row.className = 'border-b border-gray-100';
      row.innerHTML = `
            <td class="py-2 text-sm text-gray-800">${member.FullName}</td>
            <td class="py-2 text-sm text-gray-600">${member.AbsentSince || 'N/A'}</td>
            <td class="py-2"><span class="status-badge flagged">Follow up</span></td>
          `;
      welfareTableBody.appendChild(row);
    });

  } catch (error) {
    console.error('Error initializing dashboard:', error);
  }
}

// Initialize Members Page - FIXED: Added proper error handling and data refresh
async function initializeMembersPage() {
  try {
    console.log('🔄 Initializing members page...');

    // Force reload members data from API
    await MemberService.loadMembers();
    filteredMembers = [...membersData];

    console.log('🔄 Members data refreshed, total members:', membersData.length);

    const memberSearch = document.getElementById('member-search');
    const genderFilter = document.getElementById('gender-filter');
    const householdFilter = document.getElementById('household-filter');
    const rowsPerPage = document.getElementById('rows-per-page');

    // Filter members function
    function filterMembers() {
      const searchValue = memberSearch.value.toLowerCase();
      const genderValue = genderFilter.value;
      const householdValue = householdFilter.value;

      filteredMembers = membersData.filter(member => {
        // Safe property access with fallbacks
        /*const fullName = member.FullName || member.fullName || '';*/
        const email = member.Email || member.email || '';
        const phone = member.PhoneNumber || member.phoneNumber || member.phone || '';
        const gender = member.Gender || member.gender || '';
        const householdName = member.HouseholdName || member.householdName || '';

        const matchesSearch = !searchValue ||
          fullName.toLowerCase().includes(searchValue) ||
          email.toLowerCase().includes(searchValue) ||
          phone.includes(searchValue);

        const matchesGender = !genderValue || gender === genderValue;
        const matchesHousehold = !householdValue || householdName === householdValue;

        return matchesSearch && matchesGender && matchesHousehold;
      });

      currentMembersPage = 1;
      renderMembersTable();
    }

    // Render members table with pagination - FIXED: Safe property access
    function renderMembersTable() {
      const tableBody = document.getElementById('members-table-body');
      const visibleCount = document.getElementById('members-visible-count');
      const totalCount = document.getElementById('members-total-count');
      const pagination = document.getElementById('members-pagination');

      // Calculate pagination
      const startIndex = (currentMembersPage - 1) * membersPerPage;
      const endIndex = startIndex + membersPerPage;
      const currentMembers = filteredMembers.slice(startIndex, endIndex);
      const totalPages = Math.ceil(filteredMembers.length / membersPerPage);

      // Clear table
      tableBody.innerHTML = '';

      if (currentMembers.length === 0) {
        tableBody.innerHTML = `
          <tr>
            <td colspan="7" class="px-6 py-8 text-center text-gray-500">
              <i class="fas fa-users text-4xl mb-2 text-gray-300"></i>
              <p class="text-lg">No members found</p>
              <p class="text-sm">Try adjusting your search or add new members</p>
            </td>
          </tr>
        `;
      } else {
        // Populate table with safe property access
        currentMembers.forEach(member => {
          const row = document.createElement('tr');
          row.className = 'member-row hover:bg-gray-50';

          // Safe property access with consistent fallbacks
          const fullName = member.FullName || 'Unknown Member';
          const gender = member.Gender || member.gender || 'N/A';
          const dateOfBirth = member.DateOfBirth || member.dateOfBirth;
          const householdName = member.HouseholdName || member.householdName || 'No Household';
          const phoneNumber = member.PhoneNumber || member.phoneNumber || member.phone || 'N/A';
          const email = member.Email || member.email || 'N/A';
          const dateJoined = member.DateJoined || member.dateJoined;
          const immigrationStatus = member.ImmigrationStatus || member.immigrationStatus || 'N/A';
          const memberId = member.Id || member.id;

          row.innerHTML = `
    <td class="px-6 py-4 whitespace-nowrap">
      <div class="flex items-center">
        <div class="h-10 w-10 rounded-full bg-indigo-100 flex items-center justify-center text-indigo-800 font-semibold text-sm">
          <i class="fas fa-user"></i>
        </div>
        <div class="ml-4">
          <div class="text-sm font-medium text-gray-900">${fullName}</div>
          <div class="text-sm text-gray-500">${dateJoined ? new Date(dateJoined).toLocaleDateString() : 'N/A'}</div>
        </div>
      </div>
    </td>
    <td class="px-6 py-4 whitespace-nowrap text-sm text-gray-500">${gender}</td>
    <td class="px-6 py-4 whitespace-nowrap text-sm text-gray-500">${dateOfBirth ? new Date(dateOfBirth).toLocaleDateString() : 'N/A'}</td>
    <td class="px-6 py-4 whitespace-nowrap text-sm text-gray-500">${householdName}</td>
    <td class="px-6 py-4 whitespace-nowrap">
      <div class="text-sm text-gray-900">${phoneNumber}</div>
      <div class="text-sm text-gray-500">${email}</div>
    </td>
    <td class="px-6 py-4 whitespace-nowrap">
      <span class="status-badge ${immigrationStatus && immigrationStatus.includes('Expires') ? 'flagged' : 'ok'}">
        ${immigrationStatus}
      </span>
    </td>
    <td class="px-6 py-4 whitespace-nowrap text-sm text-gray-500">
      <div class="flex items-center space-x-2">
        <button class="text-indigo-600 hover:text-indigo-900 edit-member" data-member-id="${memberId}">
          <i class="fas fa-edit"></i>
        </button>
        <button class="text-gray-600 hover:text-gray-900 view-member" data-member-id="${memberId}">
          <i class="fas fa-eye"></i>
        </button>
      </div>
    </td>
  `;

          tableBody.appendChild(row);
        });
      }

      // Update counters
      visibleCount.textContent = currentMembers.length;
      totalCount.textContent = filteredMembers.length;

      // Render pagination
      renderPagination(pagination, currentMembersPage, totalPages, 'members', (page) => {
        currentMembersPage = page;
        renderMembersTable();
      });
    }

    // Initialize event listeners
    if (memberSearch) {
      memberSearch.addEventListener('input', filterMembers);
    }

    if (genderFilter) {
      genderFilter.addEventListener('change', filterMembers);
    }

    if (householdFilter) {
      householdFilter.addEventListener('change', filterMembers);
    }

    if (rowsPerPage) {
      rowsPerPage.addEventListener('change', (e) => {
        membersPerPage = parseInt(e.target.value);
        currentMembersPage = 1;
        renderMembersTable();
      });
    }

    // Initial render
    renderMembersTable();
    console.log('✅ Members page initialized successfully');
  } catch (error) {
    console.error('❌ Error initializing members page:', error);
    // Show user-friendly error message
    const tableBody = document.getElementById('members-table-body');
    if (tableBody) {
      tableBody.innerHTML = `
        <tr>
          <td colspan="7" class="px-6 py-8 text-center text-gray-500">
            <i class="fas fa-exclamation-triangle text-yellow-500 text-4xl mb-2"></i>
            <p class="text-lg">Failed to load members</p>
            <p class="text-sm">Please try refreshing the page</p>
            <button onclick="initializeMembersPage()" class="mt-2 px-4 py-2 bg-indigo-600 text-white rounded-md hover:bg-indigo-700">
              <i class="fas fa-redo mr-2"></i>Retry
            </button>
          </td>
        </tr>
      `;
    }
  }
}

// Initialize Households Page
async function initializeHouseholdsPage() {
  try {
    // Load real data from API
    await HouseholdService.loadHouseholds();
    filteredHouseholds = [...householdsData];

    const householdSearch = document.getElementById('household-search');
    const householdsContainer = document.getElementById('households-container');
    const visibleCount = document.getElementById('households-visible-count');
    const totalCount = document.getElementById('households-total-count');
    const pagination = document.getElementById('households-pagination');

    // Filter households
    function filterHouseholds() {
      const searchValue = householdSearch.value.toLowerCase();

      filteredHouseholds = householdsData.filter(household => {
        return !searchValue ||
          household.name.toLowerCase().includes(searchValue) ||
          (household.headName && household.headName.toLowerCase().includes(searchValue)) ||
          (household.address && household.address.toLowerCase().includes(searchValue));
      });

      currentHouseholdsPage = 1;
      renderHouseholds();
    }

    // Render households
    function renderHouseholds() {
      householdsContainer.innerHTML = '';

      // Calculate pagination
      const startIndex = (currentHouseholdsPage - 1) * householdsPerPage;
      const endIndex = startIndex + householdsPerPage;
      const currentHouseholds = filteredHouseholds.slice(startIndex, endIndex);
      const totalPages = Math.ceil(filteredHouseholds.length / householdsPerPage);

      currentHouseholds.forEach(household => {
        const card = document.createElement('div');
        card.className = 'bg-white rounded-lg shadow overflow-hidden';

        card.innerHTML = `
              <div class="p-6">
                <h3 class="font-bold text-lg text-gray-800 mb-2">${household.name}</h3>
                <div class="flex items-center mb-4">
                  <div class="text-sm bg-indigo-100 text-indigo-800 px-3 py-1 rounded-full">${household.memberCount || 0} members</div>
                </div>
                <div class="text-sm text-gray-600 mb-4">
                  <div class="mb-1"><strong>Head:</strong> ${household.headName || 'N/A'}</div>
                  <div class="mb-1"><strong>Address:</strong> ${household.address || 'N/A'}</div>
                  <div><strong>Phone:</strong> ${household.primaryPhone || 'N/A'}</div>
                </div>
                <div class="flex justify-between">
                  <div class="text-sm">
                    <div class="font-medium text-gray-800">Attendance Rate</div>
                    <div class="${household.attendanceRate >= 80 ? 'text-green-600' : household.attendanceRate >= 60 ? 'text-yellow-600' : 'text-red-600'}">${household.attendanceRate || 0}%</div>
                  </div>
                  <button class="view-household-details text-indigo-600 hover:text-indigo-900" data-household-id="${household.id}">
                    <i class="fas fa-chevron-right"></i> View Details
                  </button>
                </div>
              </div>
            `;

        householdsContainer.appendChild(card);
      });

      // Update counters
      visibleCount.textContent = currentHouseholds.length;
      totalCount.textContent = filteredHouseholds.length;

      // Render pagination
      renderPagination(pagination, currentHouseholdsPage, totalPages, 'households', (page) => {
        currentHouseholdsPage = page;
        renderHouseholds();
      });

      // Add event listeners to view details buttons
      document.querySelectorAll('.view-household-details').forEach(button => {
        button.addEventListener('click', function () {
          const householdId = parseInt(this.getAttribute('data-household-id'));
          showHouseholdDetails(householdId);
        });
      });
    }

    // Show household details
    function showHouseholdDetails(householdId) {
      const household = householdsData.find(h => h.id === householdId);
      if (!household) return;

      const title = document.getElementById('household-details-title');
      const content = document.getElementById('household-details-content');

      title.textContent = `${household.name} - Details`;

      // Get household members
      const householdMembers = membersData.filter(member =>
        member.HouseholdId === householdId
      );

      content.innerHTML = `
            <div class="mb-6">
              <h4 class="font-semibold text-gray-800 mb-2">Household Information</h4>
              <div class="grid grid-cols-1 md:grid-cols-2 gap-4">
                <div>
                  <p><strong>Head of Household:</strong> ${household.headName || 'N/A'}</p>
                  <p><strong>Address:</strong> ${household.address || 'N/A'}</p>
                </div>
                <div>
                  <p><strong>Phone:</strong> ${household.primaryPhone || 'N/A'}</p>
                  <p><strong>Email:</strong> ${household.email || 'N/A'}</p>
                </div>
              </div>
            </div>
            
            <div>
              <h4 class="font-semibold text-gray-800 mb-2">Household Members (${householdMembers.length})</h4>
              <div class="overflow-x-auto">
                <table class="min-w-full">
                  <thead>
                    <tr class="bg-gray-50 border-b border-gray-200">
                      <th class="px-4 py-2 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">Name</th>
                      <th class="px-4 py-2 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">Gender</th>
                      <th class="px-4 py-2 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">Phone</th>
                      <th class="px-4 py-2 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">Status</th>
                    </tr>
                  </thead>
                  <tbody>
                    ${householdMembers.map(member => `
                      <tr class="border-b border-gray-100">
                        <td class="px-4 py-2 text-sm text-gray-800">${member.FullName}</td>
                        <td class="px-4 py-2 text-sm text-gray-600">${member.Gender || 'N/A'}</td>
                        <td class="px-4 py-2 text-sm text-gray-600">${member.PhoneNumber || 'N/A'}</td>
                        <td class="px-4 py-2 text-sm text-gray-600">
                          <span class="status-badge ${member.IsFlagged ? 'flagged' : 'ok'}">${member.IsFlagged ? 'Follow up' : 'Active'}</span>
                        </td>
                      </tr>
                    `).join('')}
                  </tbody>
                </table>
              </div>
            </div>
          `;

      openModal(householdDetailsModal);
    }

    // Initialize event listeners
    if (householdSearch) {
      householdSearch.addEventListener('input', filterHouseholds);
    }

    // Initial render
    renderHouseholds();
  } catch (error) {
    console.error('Error initializing households page:', error);
    alert(`Failed to load households data: ${error.message}`);
  }
}

// Initialize Attendance Page
async function initializeAttendancePage() {
  try {
    // Load members for attendance tracking
    await MemberService.loadMembers();

    // Set default service date to today
    const serviceDateInput = document.getElementById('service-date');
    const today = new Date();
    if (serviceDateInput) {
      serviceDateInput.value = today.toLocaleDateString('en-US', { month: 'short', day: 'numeric', year: 'numeric' });
    }

    const attendanceSearch = document.getElementById('attendance-search');
    const rowsPerPage = document.getElementById('attendance-rows-per-page');
    const markAllPresentBtn = document.getElementById('mark-all-present');
    const bulkSelectBtn = document.getElementById('bulk-select-btn');
    const saveAttendanceBtn = document.getElementById('save-attendance');
    const selectAllCheckbox = document.getElementById('select-all-attendance');

    /*// Load attendance for today
    await loadAttendanceForDate(today.toISOString().split('T')[0]);*/

    // Event listener for service date change
    if (serviceDateInput) {
      serviceDateInput.addEventListener('change', async (e) => {
        await loadAttendanceForDate(e.target.value);
      });
    }

    // Search functionality
    if (attendanceSearch) {
      attendanceSearch.addEventListener('input', filterAttendance);
    }

    // Rows per page change
    if (rowsPerPage) {
      rowsPerPage.addEventListener('change', (e) => {
        attendancePerPage = parseInt(e.target.value);
        currentAttendancePage = 1;
        renderAttendanceTable();
      });
    }

    // Mark all present
    if (markAllPresentBtn) {
      markAllPresentBtn.addEventListener('click', markAllPresent);
    }

    // Bulk select functionality
    if (bulkSelectBtn) {
      bulkSelectBtn.addEventListener('click', toggleBulkSelection);
    }

    // Save attendance
    if (saveAttendanceBtn) {
      saveAttendanceBtn.addEventListener('click', saveAllAttendance);
    }

    // Select all checkbox
    if (selectAllCheckbox) {
      selectAllCheckbox.addEventListener('change', toggleSelectAllAttendance);
    }

    console.log('✅ Attendance page initialized successfully');
  } catch (error) {
    console.error('Error initializing attendance page:', error);
    alert(`Failed to initialize attendance: ${error.message}`);
  }
}

// Load attendance for specific date
async function loadAttendanceForDate(dateString) {
  try {
    const attendanceData = await AttendanceService.loadAttendance(dateString, currentAttendancePage, attendancePerPage);
    currentAttendanceData = attendanceData.attendance || [];
    renderAttendanceTable();
  } catch (error) {
    console.error('Error loading attendance:', error);
    currentAttendanceData = [];
    renderAttendanceTable();
  }
}

// Filter attendance
function filterAttendance() {
  const searchValue = document.getElementById('attendance-search').value.toLowerCase();
  renderAttendanceTable(searchValue);
}

// Render attendance table
function renderAttendanceTable(searchFilter = '') {
  const tableBody = document.getElementById('attendance-table-body');
  const visibleCount = document.getElementById('attendance-visible-count');
  const totalCount = document.getElementById('attendance-total-count');
  const pagination = document.getElementById('attendance-pagination');

  // Filter data based on search
  let filteredData = currentAttendanceData;
  if (searchFilter) {
    filteredData = currentAttendanceData.filter(member =>
      member.FullName.toLowerCase().includes(searchFilter.toLowerCase())
    );
  }

  // Calculate pagination
  const startIndex = (currentAttendancePage - 1) * attendancePerPage;
  const endIndex = startIndex + attendancePerPage;
  const currentAttendance = filteredData.slice(startIndex, endIndex);
  const totalPages = Math.ceil(filteredData.length / attendancePerPage);

  // Clear table
  tableBody.innerHTML = '';

  // Populate table
  currentAttendance.forEach((member, index) => {
    const row = document.createElement('tr');
    row.className = `attendance-row ${selectedAttendanceMembers.has(member.Id) ? 'bulk-selection-active' : ''}`;

    row.innerHTML = `
          <td class="px-6 py-4 whitespace-nowrap">
            <input type="checkbox" class="attendance-checkbox rounded border-gray-300 text-indigo-600 focus:ring-indigo-500" 
                   data-member-id="${member.Id}" ${selectedAttendanceMembers.has(member.Id) ? 'checked' : ''}>
          </td>
          <td class="px-6 py-4 whitespace-nowrap">
            <div class="flex items-center">
              <div class="h-10 w-10 rounded-full bg-indigo-100 flex items-center justify-center text-indigo-800 font-semibold text-sm">
                <i class="fas fa-user"></i>
              </div>
              <div class="ml-4">
                <div class="text-sm font-medium text-gray-900">${member.FullName}</div>
                <div class="text-sm text-gray-500">${member.HouseholdName || 'No Household'}</div>
              </div>
            </div>
          </td>
          <td class="px-6 py-4 whitespace-nowrap text-sm text-gray-500">${member.Gender}</td>
          <td class="px-6 py-4 whitespace-nowrap text-sm text-gray-500">${member.PhoneNumber || 'N/A'}</td>
          <td class="px-6 py-4 whitespace-nowrap">
            <div class="flex items-center space-x-2">
              <label class="inline-flex items-center">
                <input type="radio" name="attendance-${member.Id}" value="present" 
                       class="attendance-radio rounded-full border-gray-300 text-green-600 focus:ring-green-500" 
                       data-member-id="${member.Id}" ${member.IsPresent ? 'checked' : ''}>
                <span class="ml-2 text-sm text-gray-700">Present</span>
              </label>
              <label class="inline-flex items-center">
                <input type="radio" name="attendance-${member.Id}" value="absent" 
                       class="attendance-radio rounded-full border-gray-300 text-red-600 focus:ring-red-500"
                       data-member-id="${member.Id}" ${!member.IsPresent ? 'checked' : ''}>
                <span class="ml-2 text-sm text-gray-700">Absent</span>
              </label>
            </div>
          </td>
          <td class="px-6 py-4 whitespace-nowrap">
            <label class="inline-flex items-center">
              <input type="checkbox" class="flagged-checkbox rounded border-gray-300 text-yellow-600 focus:ring-yellow-500"
                     data-member-id="${member.Id}" ${member.IsFlagged ? 'checked' : ''}>
              <span class="ml-2 text-sm text-gray-700">Flag for follow-up</span>
            </label>
          </td>
          <td class="px-6 py-4 whitespace-nowrap">
            <input type="text" class="attendance-notes w-full px-2 py-1 border border-gray-300 rounded text-sm" 
                   placeholder="Add notes..." data-member-id="${member.Id}" value="${member.Notes || ''}">
          </td>
        `;

    tableBody.appendChild(row);
  });

  // Update counters
  visibleCount.textContent = currentAttendance.length;
  totalCount.textContent = filteredData.length;

  // Show search results info
  const searchResultsInfo = document.getElementById('search-results-info');
  if (searchFilter && searchResultsInfo) {
    searchResultsInfo.textContent = `(${filteredData.length} results)`;
    searchResultsInfo.classList.remove('hidden');
  } else if (searchResultsInfo) {
    searchResultsInfo.classList.add('hidden');
  }

  // Render pagination
  renderPagination(pagination, currentAttendancePage, totalPages, 'attendance', (page) => {
    currentAttendancePage = page;
    renderAttendanceTable(searchFilter);
  });

  // Add event listeners to checkboxes and radios
  addAttendanceEventListeners();
}

// Add event listeners to attendance elements
function addAttendanceEventListeners() {
  // Checkbox selection
  document.querySelectorAll('.attendance-checkbox').forEach(checkbox => {
    checkbox.addEventListener('change', function () {
      const memberId = parseInt(this.getAttribute('data-member-id'));
      if (this.checked) {
        selectedAttendanceMembers.add(memberId);
        this.closest('tr').classList.add('bulk-selection-active');
      } else {
        selectedAttendanceMembers.delete(memberId);
        this.closest('tr').classList.remove('bulk-selection-active');
      }
    });
  });

  // Attendance radios
  document.querySelectorAll('.attendance-radio').forEach(radio => {
    radio.addEventListener('change', function () {
      if (this.checked) {
        // Auto-save individual attendance
        saveIndividualAttendance(parseInt(this.getAttribute('data-member-id')));
      }
    });
  });

  // Flagged checkboxes
  document.querySelectorAll('.flagged-checkbox').forEach(checkbox => {
    checkbox.addEventListener('change', function () {
      // Auto-save flagged status
      saveIndividualAttendance(parseInt(this.getAttribute('data-member-id')));
    });
  });

  // Notes input with debounce
  document.querySelectorAll('.attendance-notes').forEach(input => {
    let timeout;
    input.addEventListener('input', function () {
      clearTimeout(timeout);
      timeout = setTimeout(() => {
        saveIndividualAttendance(parseInt(this.getAttribute('data-member-id')));
      }, 1000);
    });
  });
}

// Save individual attendance
async function saveIndividualAttendance(memberId) {
  try {
    const row = document.querySelector(`[data-member-id="${memberId}"]`).closest('tr');
    const isPresent = row.querySelector('input[value="present"]').checked;
    const isFlagged = row.querySelector('.flagged-checkbox').checked;
    const notes = row.querySelector('.attendance-notes').value;
    const serviceDate = document.getElementById('service-date').value;

    const attendanceData = {
      ServiceDate: serviceDate,
      Attendance: [{
        MemberId: memberId,
        IsPresent: isPresent,
        IsFlagged: isFlagged,
        Notes: notes
      }]
    };

    await AttendanceService.saveBulkAttendance(attendanceData);

    // Show success indicator
    showAutosaveIndicator('saved');
  } catch (error) {
    console.error('Error saving attendance:', error);
    showAutosaveIndicator('failed');
  }
}

// Save all attendance
async function saveAllAttendance() {
  try {
    const serviceDate = document.getElementById('service-date').value;
    const attendanceData = {
      ServiceDate: serviceDate,
      Attendance: []
    };

    document.querySelectorAll('.attendance-row').forEach(row => {
      const memberId = parseInt(row.querySelector('.attendance-checkbox').getAttribute('data-member-id'));
      const isPresent = row.querySelector('input[value="present"]').checked;
      const isFlagged = row.querySelector('.flagged-checkbox').checked;
      const notes = row.querySelector('.attendance-notes').value;

      attendanceData.Attendance.push({
        MemberId: memberId,
        IsPresent: isPresent,
        IsFlagged: isFlagged,
        Notes: notes
      });
    });

    await AttendanceService.saveBulkAttendance(attendanceData);
    showAutosaveIndicator('saved');
    alert('Attendance saved successfully!');
  } catch (error) {
    console.error('Error saving all attendance:', error);
    showAutosaveIndicator('failed');
    alert('Failed to save attendance: ' + error.message);
  }
}

// Mark all present
function markAllPresent() {
  document.querySelectorAll('.attendance-radio[value="present"]').forEach(radio => {
    radio.checked = true;
    radio.dispatchEvent(new Event('change'));
  });
}

// Toggle bulk selection
function toggleBulkSelection() {
  const isActive = document.body.classList.toggle('bulk-selection-active');
  const btn = document.getElementById('bulk-select-btn');

  if (isActive) {
    btn.innerHTML = '<i class="fas fa-times mr-2"></i> Cancel Selection';
    btn.classList.remove('bg-blue-600', 'hover:bg-blue-700');
    btn.classList.add('bg-gray-600', 'hover:bg-gray-700');
  } else {
    btn.innerHTML = '<i class="fas fa-check-square mr-2"></i> Bulk Select';
    btn.classList.remove('bg-gray-600', 'hover:bg-gray-700');
    btn.classList.add('bg-blue-600', 'hover:bg-blue-700');
    selectedAttendanceMembers.clear();
    document.querySelectorAll('.attendance-checkbox').forEach(cb => cb.checked = false);
    document.querySelectorAll('.attendance-row').forEach(row => row.classList.remove('bulk-selection-active'));
  }
}

// Toggle select all attendance
function toggleSelectAllAttendance() {
  const isChecked = document.getElementById('select-all-attendance').checked;
  document.querySelectorAll('.attendance-checkbox').forEach(checkbox => {
    checkbox.checked = isChecked;
    checkbox.dispatchEvent(new Event('change'));
  });
}

// Show autosave indicator
function showAutosaveIndicator(status) {
  const indicator = document.getElementById('autosave-indicator');
  if (!indicator) return;

  indicator.className = `autosave-indicator text-sm flex items-center ${status}`;

  if (status === 'saving') {
    indicator.innerHTML = '<i class="fas fa-sync-alt animate-spin mr-1"></i><span>Saving...</span>';
  } else if (status === 'saved') {
    indicator.innerHTML = '<i class="fas fa-check-circle text-green-500 mr-1"></i><span>Saved</span>';
    setTimeout(() => {
      indicator.className = 'autosave-indicator text-sm flex items-center';
    }, 2000);
  } else if (status === 'failed') {
    indicator.innerHTML = '<i class="fas fa-exclamation-circle text-red-500 mr-1"></i><span>Save failed</span>';
  }
}

// Initialize Reports Page
async function initializeReportsPage() {
  const generateReportBtn = document.getElementById('generate-report');
  const exportCsvBtn = document.getElementById('export-csv');
  const exportPdfBtn = document.getElementById('export-pdf');
  const exportExcelResultsBtn = document.getElementById('export-excel-results');
  const exportPdfResultsBtn = document.getElementById('export-pdf-results');

  if (generateReportBtn) {
    generateReportBtn.addEventListener('click', generateReport);
  }

  if (exportCsvBtn) {
    exportCsvBtn.addEventListener('click', exportToCSV);
  }

  if (exportPdfBtn) {
    exportPdfBtn.addEventListener('click', exportToPDF);
  }

  if (exportExcelResultsBtn) {
    exportExcelResultsBtn.addEventListener('click', exportResultsToExcel);
  }

  if (exportPdfResultsBtn) {
    exportPdfResultsBtn.addEventListener('click', exportResultsToPDF);
  }
}

// Initialize Settings Page
async function initializeSettingsPage() {
  try {
    // Load users for user management
    await UserService.loadUsers();

    const usersList = document.getElementById('users-list');
    usersList.innerHTML = '';

    // Populate users list
    usersData.forEach(user => {
      const userDiv = document.createElement('div');
      userDiv.className = 'p-3 border border-gray-200 rounded-md';

      let roleBadge = '';
      if (user.RoleLevel === 3) {
        roleBadge = '<div class="text-xs px-2 py-1 bg-indigo-100 text-indigo-800 rounded-full">Administrator</div>';
      } else if (user.RoleLevel === 2) {
        roleBadge = '<div class="text-xs px-2 py-1 bg-green-100 text-green-800 rounded-full">Attendance Taker</div>';
      } else {
        roleBadge = '<div class="text-xs px-2 py-1 bg-gray-100 text-gray-800 rounded-full">View Only</div>';
      }

      userDiv.innerHTML = `
            <div class="flex justify-between items-center">
              <div>
                <div class="font-medium">${user.FullName}</div>
                <div class="text-sm text-gray-500">${user.Email}</div>
              </div>
              ${roleBadge}
            </div>
            <div class="mt-2 flex space-x-2">
              <button class="text-xs text-indigo-600 hover:text-indigo-900" onclick="resetUserPassword(${user.Id})">
                <i class="fas fa-key mr-1"></i>Reset Password
              </button>
              <button class="text-xs text-red-600 hover:text-red-900" onclick="deleteUser(${user.Id})">
                <i class="fas fa-trash mr-1"></i>Delete
              </button>
            </div>
          `;

      usersList.appendChild(userDiv);
    });
  } catch (error) {
    console.error('Error initializing settings page:', error);
  }
}

// Report Generation Functions
function generateReport() {
  const reportType = document.getElementById('report-type').value;
  const startDate = document.getElementById('report-start-date').value;
  const endDate = document.getElementById('report-end-date').value;

  const resultsBody = document.getElementById('report-results-body');
  const resultsTable = document.getElementById('report-results-table');
  const thead = resultsTable.querySelector('thead tr');

  // Clear previous results
  resultsBody.innerHTML = '';
  thead.innerHTML = '';

  let headers = [];
  let data = [];

  switch (reportType) {
    case 'attendance':
      headers = ['Date', 'Total Members', 'Present', 'Absent', 'Attendance Rate'];
      data = [
        ['May 13, 2025', '243', '189', '54', '78%'],
        ['May 6, 2025', '242', '195', '47', '81%'],
        ['Apr 29, 2025', '240', '180', '60', '75%']
      ];
      break;
    case 'members':
      headers = ['First Name', 'Last Name', 'Full Name', 'Gender', 'Household', 'Phone', 'Status'];
      data = membersData.slice(0, 5).map(member => [
        member.FirstName || 'N/A',
        member.LastName || 'N/A',
        member.FullName,
        member.Gender || 'N/A',
        member.HouseholdName || 'N/A',
        member.PhoneNumber || 'N/A',
        member.ImmigrationStatus || 'N/A'
      ]);
      break;
    case 'households':
      headers = ['Household Name', 'Head', 'Members', 'Address', 'Attendance Rate'];
      data = householdsData.map(household => [
        household.name,
        household.headName || 'N/A',
        household.memberCount || 0,
        household.address || 'N/A',
        `${household.attendanceRate || 0}%`
      ]);
      break;
    case 'flagged':
      headers = ['Name', 'Absent Since', 'Contact', 'Welfare Member'];
      data = membersData
        .filter(m => m.IsFlagged)
        .slice(0, 5)
        .map(member => [
          member.FullName,
          member.AbsentSince || 'N/A',
          member.PhoneNumber || 'N/A',
          member.WelfareMemberName || 'N/A'
        ]);
      break;
    case 'immigration':
      headers = ['Name', 'Document Type', 'Expiry Date', 'Days Left'];
      data = membersData
        .filter(m => m.ImmigrationStatus && m.ImmigrationStatus.includes('Expires'))
        .slice(0, 5)
        .map(member => [
          member.FullName,
          member.ImmigrationStatus || 'N/A',
          member.DocumentExpiry || 'N/A',
          member.daysLeft || 'N/A'
        ]);
      break;
    case 'birthdays':
      headers = ['Name', 'Date of Birth', 'Upcoming Birthday', 'Age', 'Contact'];
      const today = new Date();
      data = membersData
        .filter(m => m.DateOfBirth)
        .slice(0, 5)
        .map(member => {
          const birthDate = new Date(member.DateOfBirth);
          const nextBirthday = new Date(today.getFullYear(), birthDate.getMonth(), birthDate.getDate());
          const age = today.getFullYear() - birthDate.getFullYear();
          return [
            member.FullName,
            birthDate.toLocaleDateString(),
            nextBirthday.toLocaleDateString(),
            age,
            member.PhoneNumber || 'N/A'
          ];
        });
      break;
  }

  // Add headers
  headers.forEach(header => {
    const th = document.createElement('th');
    th.className = 'px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider';
    th.textContent = header;
    thead.appendChild(th);
  });

  // Add data
  data.forEach(rowData => {
    const tr = document.createElement('tr');
    tr.className = 'border-b border-gray-200';

    rowData.forEach(cellData => {
      const td = document.createElement('td');
      td.className = 'px-6 py-3 whitespace-nowrap text-sm text-gray-800';
      td.textContent = cellData;
      tr.appendChild(td);
    });

    resultsBody.appendChild(tr);
  });
}

// Export Functions
function exportToCSV() {
  const reportType = document.getElementById('report-type').value;
  alert(`Exporting ${reportType} report as CSV...`);
  // Implementation would convert current report data to CSV and download
}

function exportToPDF() {
  const reportType = document.getElementById('report-type').value;
  alert(`Exporting ${reportType} report as PDF...`);
  // Implementation would use jsPDF to generate PDF
}

function exportResultsToExcel() {
  alert('Exporting results to Excel...');
}

function exportResultsToPDF() {
  alert('Exporting results to PDF...');
}

// Pagination Helper
function renderPagination(container, currentPage, totalPages, pageType, onPageChange) {
  container.innerHTML = '';

  if (totalPages <= 1) return;

  // Previous button
  const prevButton = document.createElement('button');
  prevButton.className = `px-3 py-2 rounded-md border border-gray-300 bg-white text-sm font-medium ${currentPage === 1 ? 'text-gray-400 cursor-not-allowed' : 'text-gray-700 hover:bg-gray-50'}`;
  prevButton.innerHTML = '<i class="fas fa-chevron-left mr-1"></i> Previous';
  prevButton.disabled = currentPage === 1;
  prevButton.addEventListener('click', () => {
    if (currentPage > 1) {
      onPageChange(currentPage - 1);
    }
  });
  container.appendChild(prevButton);

  // Page numbers - show limited pages with ellipsis
  const maxVisiblePages = 5;
  let startPage = Math.max(1, currentPage - Math.floor(maxVisiblePages / 2));
  let endPage = Math.min(totalPages, startPage + maxVisiblePages - 1);

  if (endPage - startPage + 1 < maxVisiblePages) {
    startPage = Math.max(1, endPage - maxVisiblePages + 1);
  }

  // First page + ellipsis
  if (startPage > 1) {
    const firstPageButton = document.createElement('button');
    firstPageButton.className = `px-3 py-2 rounded-md border border-gray-300 bg-white text-sm font-medium ${1 === currentPage ? 'bg-indigo-600 text-white' : 'text-gray-700 hover:bg-gray-50'}`;
    firstPageButton.textContent = '1';
    firstPageButton.addEventListener('click', () => onPageChange(1));
    container.appendChild(firstPageButton);

    if (startPage > 2) {
      const ellipsis = document.createElement('span');
      ellipsis.className = 'px-3 py-2 text-sm text-gray-500';
      ellipsis.textContent = '...';
      container.appendChild(ellipsis);
    }
  }

  // Page numbers
  for (let i = startPage; i <= endPage; i++) {
    const pageButton = document.createElement('button');
    pageButton.className = `px-3 py-2 rounded-md text-sm font-medium ${currentPage === i ? 'bg-indigo-600 text-white' : 'border border-gray-300 bg-white text-gray-700 hover:bg-gray-50'}`;
    pageButton.textContent = i;
    pageButton.addEventListener('click', () => onPageChange(i));
    container.appendChild(pageButton);
  }

  // Last page + ellipsis
  if (endPage < totalPages) {
    if (endPage < totalPages - 1) {
      const ellipsis = document.createElement('span');
      ellipsis.className = 'px-3 py-2 text-sm text-gray-500';
      ellipsis.textContent = '...';
      container.appendChild(ellipsis);
    }

    const lastPageButton = document.createElement('button');
    lastPageButton.className = `px-3 py-2 rounded-md border border-gray-300 bg-white text-sm font-medium ${totalPages === currentPage ? 'bg-indigo-600 text-white' : 'text-gray-700 hover:bg-gray-50'}`;
    lastPageButton.textContent = totalPages;
    lastPageButton.addEventListener('click', () => onPageChange(totalPages));
    container.appendChild(lastPageButton);
  }

  // Next button
  const nextButton = document.createElement('button');
  nextButton.className = `px-3 py-2 rounded-md border border-gray-300 bg-white text-sm font-medium ${currentPage === totalPages ? 'text-gray-400 cursor-not-allowed' : 'text-gray-700 hover:bg-gray-50'}`;
  nextButton.innerHTML = 'Next <i class="fas fa-chevron-right ml-1"></i>';
  nextButton.disabled = currentPage === totalPages;
  nextButton.addEventListener('click', () => {
    if (currentPage < totalPages) {
      onPageChange(currentPage + 1);
    }
  });
  container.appendChild(nextButton);
}

// Chart management functions
function destroyCharts() {
  Object.values(chartInstances).forEach(chart => {
    if (chart) {
      chart.destroy();
    }
  });
  chartInstances = {};
}

// Initialize Charts
function initCharts() {
  destroyCharts(); // Destroy existing charts first

  // Dashboard Attendance Chart - Enhanced
  if (document.getElementById('attendance-chart')) {
    const ctx = document.getElementById('attendance-chart').getContext('2d');
    chartInstances.attendanceChart = new Chart(ctx, {
      type: 'bar',
      data: {
        labels: ['Apr 15', 'Apr 22', 'Apr 29', 'May 6', 'May 13'],
        datasets: [
          {
            label: 'Present',
            data: [189, 195, 180, 195, 189],
            backgroundColor: 'rgb(34, 197, 94)',
            borderColor: 'rgb(34, 197, 94)',
            borderWidth: 1
          },
          {
            label: 'Absent',
            data: [54, 47, 60, 47, 54],
            backgroundColor: 'rgb(239, 68, 68)',
            borderColor: 'rgb(239, 68, 68)',
            borderWidth: 1
          }
        ]
      },
      options: {
        responsive: true,
        maintainAspectRatio: false,
        scales: {
          y: {
            beginAtZero: true,
            title: {
              display: true,
              text: 'Number of Members'
            }
          },
          x: {
            title: {
              display: true,
              text: 'Service Dates'
            }
          }
        },
        plugins: {
          title: {
            display: true,
            text: 'Weekly Attendance Breakdown'
          },
          tooltip: {
            mode: 'index',
            intersect: false
          }
        }
      }
    });
  }

  // Demographics Chart
  if (document.getElementById('demographics-chart')) {
    const ctx = document.getElementById('demographics-chart').getContext('2d');
    chartInstances.demographicsChart = new Chart(ctx, {
      type: 'doughnut',
      data: {
        labels: ['Male', 'Female'],
        datasets: [{
          data: [105, 138],
          backgroundColor: [
            'rgb(59, 130, 246)',
            'rgb(236, 72, 153)'
          ]
        }]
      },
      options: {
        responsive: true,
        maintainAspectRatio: false,
        plugins: {
          legend: {
            position: 'bottom'
          },
          title: {
            display: true,
            text: 'Gender Distribution'
          }
        }
      }
    });
  }

  // Reports Page Charts
  if (document.getElementById('attendance-percentage-chart')) {
    const ctx = document.getElementById('attendance-percentage-chart').getContext('2d');
    chartInstances.attendancePercentageChart = new Chart(ctx, {
      type: 'line',
      data: {
        labels: ['Apr 15', 'Apr 22', 'Apr 29', 'May 6', 'May 13'],
        datasets: [{
          label: 'Attendance Rate',
          data: [78, 81, 75, 81, 78],
          borderColor: 'rgb(79, 70, 229)',
          backgroundColor: 'rgba(79, 70, 229, 0.1)',
          fill: true,
          tension: 0.3
        }]
      },
      options: {
        responsive: true,
        maintainAspectRatio: false,
        scales: {
          y: {
            beginAtZero: true,
            max: 100,
            ticks: {
              callback: function (value) {
                return value + '%';
              }
            }
          }
        },
        plugins: {
          tooltip: {
            callbacks: {
              label: function (context) {
                return context.dataset.label + ': ' + context.raw + '%';
              }
            }
          }
        }
      }
    });
  }
}

// Birthday Wishes Function
function sendBirthdayWish(name, email, phone) {
  if (confirm(`Send birthday wish to ${name}?`)) {
    // This would integrate with your email/SMS service
    console.log(`Sending birthday wish to ${name} (${email}, ${phone})`);
    alert(`Birthday wish sent to ${name}!`);

    // In a real implementation, you would call your backend API
    // fetch('/api/notifications/birthday', {
    //   method: 'POST',
    //   body: JSON.stringify({ name, email, phone })
    // });
  }
}

// User Management Functions
function resetUserPassword(userId) {
  const newPassword = prompt('Enter new password for this user:');
  if (newPassword && newPassword.length >= 6) {
    // This would call your backend API
    console.log(`Resetting password for user ${userId}`);
    alert('Password reset successfully!');
  } else if (newPassword) {
    alert('Password must be at least 6 characters long');
  }
}

function deleteUser(userId) {
  if (confirm('Are you sure you want to delete this user?')) {
    // This would call your backend API
    console.log(`Deleting user ${userId}`);
    alert('User deleted successfully!');
    // Refresh the users list
    initializeSettingsPage();
  }
}

// CSV Upload Functionality
document.addEventListener('DOMContentLoaded', function () {
  // DOM Elements
  const uploadCsvBtn = document.getElementById('upload-csv-btn');
  const csvUploadModal = document.getElementById('csv-upload-modal');
  const closeCsvModal = document.getElementById('close-csv-modal');
  const cancelUpload = document.getElementById('cancel-upload');
  const startUpload = document.getElementById('start-upload');
  const csvFileInput = document.getElementById('csv-file-input');
  const csvDropZone = document.getElementById('csv-drop-zone');
  const fileName = document.getElementById('file-name');
  const uploadProgress = document.getElementById('upload-progress');
  const progressBar = document.getElementById('progress-bar');
  const progressPercentage = document.getElementById('progress-percentage');
  const uploadStatus = document.getElementById('upload-status');
  const uploadSuccess = document.getElementById('upload-success');
  const uploadError = document.getElementById('upload-error');
  const errorMessage = document.getElementById('error-message');

  // Open Modal
  if (uploadCsvBtn) {
    uploadCsvBtn.addEventListener('click', () => {
      openModal(csvUploadModal);
      resetUploadForm();
    });
  }

  // Close Modal
  if (closeCsvModal) {
    closeCsvModal.addEventListener('click', () => {
      closeModal(csvUploadModal);
      resetUploadForm();
    });
  }

  // Cancel Upload
  if (cancelUpload) {
    cancelUpload.addEventListener('click', () => {
      closeModal(csvUploadModal);
      resetUploadForm();
    });
  }

  // File Input Change
  if (csvFileInput) {
    csvFileInput.addEventListener('change', handleFileSelect);
  }

  // Drag and Drop Functionality
  if (csvDropZone) {
    // Prevent default drag behaviors
    ['dragenter', 'dragover', 'dragleave', 'drop'].forEach(eventName => {
      csvDropZone.addEventListener(eventName, preventDefaults, false);
    });

    // Highlight drop zone when item is dragged over it
    ['dragenter', 'dragover'].forEach(eventName => {
      csvDropZone.addEventListener(eventName, highlight, false);
    });

    ['dragleave', 'drop'].forEach(eventName => {
      csvDropZone.addEventListener(eventName, unhighlight, false);
    });

    // Handle dropped files
    csvDropZone.addEventListener('drop', handleDrop, false);

    // Click to select file
    csvDropZone.addEventListener('click', () => {
      csvFileInput.click();
    });
  }

  // Start Upload
  if (startUpload) {
    startUpload.addEventListener('click', startUploadProcess);
  }

  function preventDefaults(e) {
    e.preventDefault();
    e.stopPropagation();
  }

  function highlight() {
    csvDropZone.classList.add('border-indigo-400', 'bg-indigo-50');
  }

  function unhighlight() {
    csvDropZone.classList.remove('border-indigo-400', 'bg-indigo-50');
  }

  function handleDrop(e) {
    const dt = e.dataTransfer;
    const files = dt.files;

    if (files.length > 0) {
      csvFileInput.files = files;
      handleFileSelect();
    }
  }

  function handleFileSelect() {
    const file = csvFileInput.files[0];

    if (!file) {
      resetFileSelection();
      return;
    }

    // Validate file type
    if (!file.name.toLowerCase().endsWith('.csv')) {
      showError('Please select a CSV file');
      resetFileSelection();
      return;
    }

    // Validate file size (25MB max)
    if (file.size > 25 * 1024 * 1024) {
      showError('File size exceeds 25MB limit');
      resetFileSelection();
      return;
    }

    // Show file name and enable upload button
    fileName.textContent = `Selected: ${file.name} (${formatFileSize(file.size)})`;
    fileName.classList.remove('hidden');
    startUpload.disabled = false;

    hideError();
    hideSuccess();
  }

  function resetFileSelection() {
    csvFileInput.value = '';
    fileName.classList.add('hidden');
    startUpload.disabled = true;
  }

  function resetUploadForm() {
    resetFileSelection();
    hideProgress();
    hideError();
    hideSuccess();
    uploadProgress.classList.add('hidden');
  }

  async function startUploadProcess() {
    const file = csvFileInput.files[0];

    if (!file) {
      showError('Please select a file first');
      return;
    }

    // Disable buttons during upload
    startUpload.disabled = true;
    startUpload.classList.add('btn-loading');
    cancelUpload.disabled = true;

    // Show progress
    uploadProgress.classList.remove('hidden');
    hideError();
    hideSuccess();

    try {
      // Upload file to API
      const result = await MemberService.uploadCSV(file);

      // Show success message
      hideProgress();
      showSuccess();

      // Re-enable cancel button
      cancelUpload.disabled = false;

      // Update button to "Done"
      startUpload.innerHTML = '<i class="fas fa-check mr-2"></i>Done';
      startUpload.disabled = false;
      startUpload.classList.remove('btn-loading');
      startUpload.removeEventListener('click', startUploadProcess);
      startUpload.addEventListener('click', () => {
        closeModal(csvUploadModal);
        resetUploadForm();
        // Refresh members list
        initializeMembersPage();
        alert(`${result.successCount || 'Multiple'} members imported successfully!${result.errorCount > 0 ? ` ${result.errorCount} errors occurred.` : ''}`);
      });
    } catch (error) {
      console.error('Upload failed:', error);
      showError('Upload failed: ' + error.message);
      cancelUpload.disabled = false;
      startUpload.disabled = false;
      startUpload.classList.remove('btn-loading');
    }
  }

  function showSuccess() {
    uploadSuccess.classList.remove('hidden');
  }

  function hideSuccess() {
    uploadSuccess.classList.add('hidden');
  }

  function showError(message) {
    errorMessage.textContent = message;
    uploadError.classList.remove('hidden');
  }

  function hideError() {
    uploadError.classList.add('hidden');
  }

  function hideProgress() {
    uploadProgress.classList.add('hidden');
  }

  function formatFileSize(bytes) {
    if (bytes === 0) return '0 Bytes';
    const k = 1024;
    const sizes = ['Bytes', 'KB', 'MB', 'GB'];
    const i = Math.floor(Math.log(bytes) / Math.log(k));
    return parseFloat((bytes / Math.pow(k, i)).toFixed(2)) + ' ' + sizes[i];
  }
});

// Make functions globally available
window.showPage = showPage;
window.sendBirthdayWish = sendBirthdayWish;
window.resetUserPassword = resetUserPassword;
window.deleteUser = deleteUser;
window.initializeMembersPage = initializeMembersPage;
