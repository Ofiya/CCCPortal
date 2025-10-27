import React, { useState, useEffect } from 'react';

function App() {
  const [users, setUsers] = useState([]);
  const [members, setMembers] = useState([]);
  const [households, setHouseholds] = useState([]);
  const [activeTab, setActiveTab] = useState('dashboard');
  const [showAddUser, setShowAddUser] = useState(false);
  const [showAddMember, setShowAddMember] = useState(false);
  const [showAddHousehold, setShowAddHousehold] = useState(false);
  
  const [newUser, setNewUser] = useState({
    fullName: '',
    email: '',
    password: '',
    roleLevel: 2,
    isActive: true
  });
  
  const [newMember, setNewMember] = useState({
    firstName: '',
    lastName: '',
    fullName: '',
    email: '',
    phoneNumber: '',
    membershipStatus: 'Active',
    householdId: null
  });
  
  const [newHousehold, setNewHousehold] = useState({
    name: '',
    address: '',
    city: '',
    state: '',
    zipCode: ''
  });

  const API_BASE_URL = 'https://redemptionfe.azurewebsites.net/api';

  // Fetch data based on active tab
  useEffect(() => {
    if (activeTab === 'users') {
      fetchUsers();
    } else if (activeTab === 'members') {
      fetchMembers();
      fetchHouseholds();
    } else if (activeTab === 'households') {
      fetchHouseholds();
    }
  }, [activeTab]);

  const fetchUsers = () => {
    fetch(`${API_BASE_URL}/users`)
      .then(response => {
        if (!response.ok) {
          throw new Error('Network response was not ok');
        }
        return response.json();
      })
      .then(data => setUsers(data))
      .catch(error => console.error('Error fetching users:', error));
  };

  const fetchMembers = () => {
    fetch(`${API_BASE_URL}/members`)
      .then(response => {
        if (!response.ok) {
          throw new Error('Network response was not ok');
        }
        return response.json();
      })
      .then(data => {
        if (data.members) {
          setMembers(data.members);
        } else {
          setMembers(data);
        }
      })
      .catch(error => console.error('Error fetching members:', error));
  };

  const fetchHouseholds = () => {
    fetch(`${API_BASE_URL}/households`)
      .then(response => {
        if (!response.ok) {
          if (response.status === 404) {
            console.log('Households endpoint not available');
            setHouseholds([]);
            return;
          }
          throw new Error('Network response was not ok');
        }
        return response.json();
      })
      .then(data => {
        if (data.households) {
          setHouseholds(data.households);
        } else {
          setHouseholds(data || []);
        }
      })
      .catch(error => {
        console.error('Error fetching households:', error);
        setHouseholds([]);
      });
  };

  const handleAddUser = async (e) => {
    e.preventDefault();
    try {
      const response = await fetch(`${API_BASE_URL}/users`, {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
        },
        body: JSON.stringify(newUser)
      });

      if (response.ok) {
        setNewUser({
          fullName: '',
          email: '',
          password: '',
          roleLevel: 2,
          isActive: true
        });
        setShowAddUser(false);
        fetchUsers();
      } else {
        const errorText = await response.text();
        console.error('Failed to add user. Status:', response.status, 'Response:', errorText);
        throw new Error(`HTTP error! status: ${response.status}`);
      }
    } catch (error) {
      console.error('Error adding user:', error);
      alert('Failed to add user. Please check the console for details.');
    }
  };

  const handleAddMember = async (e) => {
    e.preventDefault();
    try {
      const memberData = {
        firstName: newMember.firstName,
        lastName: newMember.lastName,
        fullName: newMember.fullName,
        email: newMember.email,
        phoneNumber: newMember.phoneNumber,
        membershipStatus: newMember.membershipStatus,
        householdId: newMember.householdId
      };

      console.log('Sending member data:', memberData);

      const response = await fetch(`${API_BASE_URL}/members`, {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
        },
        body: JSON.stringify(memberData)
      });

      if (response.ok) {
        const result = await response.json();
        console.log('Member added successfully:', result);
        setNewMember({
          firstName: '',
          lastName: '',
          fullName: '',
          email: '',
          phoneNumber: '',
          membershipStatus: 'Active',
          householdId: null
        });
        setShowAddMember(false);
        fetchMembers();
      } else {
        const errorText = await response.text();
        console.error('Failed to add member. Status:', response.status, 'Response:', errorText);
        throw new Error(`HTTP error! status: ${response.status}`);
      }
    } catch (error) {
      console.error('Error adding member:', error);
      alert('Failed to add member. Please check the console for details.');
    }
  };

  const handleAddHousehold = async (e) => {
    e.preventDefault();
    try {
      const response = await fetch(`${API_BASE_URL}/households`, {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
        },
        body: JSON.stringify(newHousehold)
      });

      if (response.ok) {
        setNewHousehold({
          name: '',
          address: '',
          city: '',
          state: '',
          zipCode: ''
        });
        setShowAddHousehold(false);
        fetchHouseholds();
      } else {
        console.error('Failed to add household. Status:', response.status);
        throw new Error(`HTTP error! status: ${response.status}`);
      }
    } catch (error) {
      console.error('Error adding household:', error);
      alert('Failed to add household. The households endpoint might not be implemented yet.');
    }
  };

  const handleCSVUpload = async (event) => {
    const file = event.target.files[0];
    if (!file) return;

    const formData = new FormData();
    formData.append('csvFile', file);

    try {
      const response = await fetch(`${API_BASE_URL}/members/upload`, {
        method: 'POST',
        body: formData,
      });

      if (response.ok) {
        alert('CSV uploaded successfully!');
        fetchMembers();
      } else {
        throw new Error('CSV upload failed');
      }
    } catch (error) {
      console.error('Error uploading CSV:', error);
      alert('Failed to upload CSV. This feature might not be implemented yet.');
    }
  };

  // Render different content based on active tab
  const renderContent = () => {
    switch (activeTab) {
      case 'dashboard':
        return (
          <>
            <h2 className="text-2xl font-bold mb-6">Dashboard</h2>
            
            {/* Stats Cards */}
            <div className="grid grid-cols-1 md:grid-cols-4 gap-4 mb-6">
              <div className="bg-white rounded-lg shadow p-4">
                <h3 className="text-lg font-semibold text-gray-700">Total Members</h3>
                <p className="text-2xl font-bold text-indigo-600">{members.length}</p>
              </div>
              <div className="bg-white rounded-lg shadow p-4">
                <h3 className="text-lg font-semibold text-gray-700">Active Members</h3>
                <p className="text-2xl font-bold text-green-600">
                  {members.filter(member => member.membershipStatus === 'Active').length}
                </p>
              </div>
              <div className="bg-white rounded-lg shadow p-4">
                <h3 className="text-lg font-semibold text-gray-700">Total Households</h3>
                <p className="text-2xl font-bold text-blue-600">{households.length}</p>
              </div>
              <div className="bg-white rounded-lg shadow p-4">
                <h3 className="text-lg font-semibold text-gray-700">System Users</h3>
                <p className="text-2xl font-bold text-purple-600">{users.length}</p>
              </div>
            </div>

            {/* Recent Members */}
            <div className="bg-white rounded-lg shadow mb-6">
              <div className="p-4 border-b flex justify-between items-center">
                <h3 className="text-lg font-semibold">Recent Members</h3>
                <button 
                  onClick={() => setActiveTab('members')}
                  className="text-indigo-600 hover:text-indigo-800"
                >
                  View All
                </button>
              </div>
              <div className="overflow-x-auto">
                <table className="min-w-full">
                  <thead className="bg-gray-50">
                    <tr>
                      <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">Name</th>
                      <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">Email</th>
                      <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">Status</th>
                    </tr>
                  </thead>
                  <tbody className="bg-white divide-y divide-gray-200">
                    {members.slice(0, 5).map(member => (
                      <tr key={member.id} className="hover:bg-gray-50">
                        <td className="px-6 py-4 whitespace-nowrap">
                          <div className="font-medium text-gray-900">
                            {member.firstName} {member.lastName}
                          </div>
                        </td>
                        <td className="px-6 py-4 whitespace-nowrap text-sm text-gray-500">
                          {member.email}
                        </td>
                        <td className="px-6 py-4 whitespace-nowrap">
                          <span className={`px-2 inline-flex text-xs leading-5 font-semibold rounded-full ${
                            member.membershipStatus === 'Active' 
                              ? 'bg-green-100 text-green-800' 
                              : 'bg-yellow-100 text-yellow-800'
                          }`}>
                            {member.membershipStatus}
                          </span>
                        </td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
            </div>
          </>
        );

      case 'users':
        return (
          <>
            <div className="flex justify-between items-center mb-6">
              <h2 className="text-2xl font-bold">System Users</h2>
              <button 
                onClick={() => setShowAddUser(true)}
                className="bg-purple-600 text-white px-4 py-2 rounded-lg hover:bg-purple-700"
              >
                Add User
              </button>
            </div>

            {/* Users Table */}
            <div className="bg-white rounded-lg shadow">
              <div className="overflow-x-auto">
                <table className="min-w-full">
                  <thead className="bg-gray-50">
                    <tr>
                      <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">Name</th>
                      <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">Email</th>
                      <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">Role</th>
                      <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">Status</th>
                      <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">Last Login</th>
                    </tr>
                  </thead>
                  <tbody className="bg-white divide-y divide-gray-200">
                    {users.map(user => (
                      <tr key={user.id} className="hover:bg-gray-50">
                        <td className="px-6 py-4 whitespace-nowrap font-medium text-gray-900">
                          {user.fullName}
                        </td>
                        <td className="px-6 py-4 whitespace-nowrap text-sm text-gray-500">
                          {user.email}
                        </td>
                        <td className="px-6 py-4 whitespace-nowrap text-sm text-gray-500">
                          {user.roleLevel === 3 ? 'Administrator' : 'Staff'}
                        </td>
                        <td className="px-6 py-4 whitespace-nowrap">
                          <span className={`px-2 inline-flex text-xs leading-5 font-semibold rounded-full ${
                            user.isActive 
                              ? 'bg-green-100 text-green-800' 
                              : 'bg-red-100 text-red-800'
                          }`}>
                            {user.isActive ? 'Active' : 'Inactive'}
                          </span>
                        </td>
                        <td className="px-6 py-4 whitespace-nowrap text-sm text-gray-500">
                          {user.lastLogin ? new Date(user.lastLogin).toLocaleDateString() : 'Never'}
                        </td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
            </div>
          </>
        );

      case 'members':
        return (
          <>
            <div className="flex justify-between items-center mb-6">
              <h2 className="text-2xl font-bold">Church Members</h2>
              <div className="space-x-2">
                <label className="bg-purple-600 text-white px-4 py-2 rounded-lg hover:bg-purple-700 cursor-pointer">
                  Upload CSV
                  <input
                    type="file"
                    accept=".csv"
                    onChange={handleCSVUpload}
                    className="hidden"
                  />
                </label>
                <button 
                  onClick={() => setShowAddMember(true)}
                  className="bg-indigo-600 text-white px-4 py-2 rounded-lg hover:bg-indigo-700"
                >
                  Add Member
                </button>
              </div>
            </div>

            {/* Members Table */}
            <div className="bg-white rounded-lg shadow">
              <div className="overflow-x-auto">
                <table className="min-w-full">
                  <thead className="bg-gray-50">
                    <tr>
                      <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">Name</th>
                      <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">Email</th>
                      <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">Phone</th>
                      <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">Status</th>
                      <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">Household</th>
                    </tr>
                  </thead>
                  <tbody className="bg-white divide-y divide-gray-200">
                    {members.map(member => (
                      <tr key={member.id} className="hover:bg-gray-50">
                        <td className="px-6 py-4 whitespace-nowrap">
                          <div className="font-medium text-gray-900">
                            {member.firstName} {member.lastName}
                          </div>
                        </td>
                        <td className="px-6 py-4 whitespace-nowrap text-sm text-gray-500">
                          {member.email}
                        </td>
                        <td className="px-6 py-4 whitespace-nowrap text-sm text-gray-500">
                          {member.phoneNumber}
                        </td>
                        <td className="px-6 py-4 whitespace-nowrap">
                          <span className={`px-2 inline-flex text-xs leading-5 font-semibold rounded-full ${
                            member.membershipStatus === 'Active' 
                              ? 'bg-green-100 text-green-800' 
                              : 'bg-yellow-100 text-yellow-800'
                          }`}>
                            {member.membershipStatus}
                          </span>
                        </td>
                        <td className="px-6 py-4 whitespace-nowrap text-sm text-gray-500">
                          {member.householdName || 'None'}
                        </td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
            </div>
          </>
        );

      case 'households':
        return (
          <>
            <div className="flex justify-between items-center mb-6">
              <h2 className="text-2xl font-bold">Households</h2>
              <button 
                onClick={() => setShowAddHousehold(true)}
                className="bg-green-600 text-white px-4 py-2 rounded-lg hover:bg-green-700"
              >
                Add Household
              </button>
            </div>

            {/* Households Table */}
            <div className="bg-white rounded-lg shadow">
              <div className="overflow-x-auto">
                <table className="min-w-full">
                  <thead className="bg-gray-50">
                    <tr>
                      <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">Household Name</th>
                      <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">Address</th>
                      <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">City</th>
                      <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">State</th>
                      <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">ZIP Code</th>
                      <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">Members</th>
                    </tr>
                  </thead>
                  <tbody className="bg-white divide-y divide-gray-200">
                    {households.map(household => (
                      <tr key={household.id} className="hover:bg-gray-50">
                        <td className="px-6 py-4 whitespace-nowrap font-medium text-gray-900">
                          {household.name}
                        </td>
                        <td className="px-6 py-4 whitespace-nowrap text-sm text-gray-500">
                          {household.address}
                        </td>
                        <td className="px-6 py-4 whitespace-nowrap text-sm text-gray-500">
                          {household.city}
                        </td>
                        <td className="px-6 py-4 whitespace-nowrap text-sm text-gray-500">
                          {household.state}
                        </td>
                        <td className="px-6 py-4 whitespace-nowrap text-sm text-gray-500">
                          {household.zipCode}
                        </td>
                        <td className="px-6 py-4 whitespace-nowrap text-sm text-gray-500">
                          {household.memberCount || 0}
                        </td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
            </div>
          </>
        );

      default:
        return null;
    }
  };

  return (
    <div className="bg-gray-100">
      <div id="app" className="h-screen flex flex-col">
        <header className="bg-white shadow-sm z-10">
          <div className="flex justify-between items-center px-4 py-3">
            <div className="flex items-center">
              <button className="mr-3 text-gray-600 hover:text-indigo-600 focus:outline-none">
                <i className="fas fa-bars text-xl"></i>
              </button>
              <h1 className="text-xl font-semibold text-gray-800">
                Redemption Parish Membership Management
              </h1>
            </div>
          </div>
        </header>

        <div className="flex flex-1 overflow-hidden">
          <nav className="sidebar bg-indigo-800 text-white w-64">
            <div className="p-4">
              <span className="font-semibold text-lg">Redemption Parish</span>
              <ul className="mt-4 space-y-2">
                <li 
                  className={`p-2 rounded cursor-pointer ${activeTab === 'dashboard' ? 'bg-indigo-700' : 'hover:bg-indigo-700'}`}
                  onClick={() => setActiveTab('dashboard')}
                >
                  Dashboard
                </li>
                <li 
                  className={`p-2 rounded cursor-pointer ${activeTab === 'users' ? 'bg-indigo-700' : 'hover:bg-indigo-700'}`}
                  onClick={() => setActiveTab('users')}
                >
                  System Users
                </li>
                <li 
                  className={`p-2 rounded cursor-pointer ${activeTab === 'members' ? 'bg-indigo-700' : 'hover:bg-indigo-700'}`}
                  onClick={() => setActiveTab('members')}
                >
                  Church Members
                </li>
                <li 
                  className={`p-2 rounded cursor-pointer ${activeTab === 'households' ? 'bg-indigo-700' : 'hover:bg-indigo-700'}`}
                  onClick={() => setActiveTab('households')}
                >
                  Households
                </li>
                <li className="hover:bg-indigo-700 p-2 rounded">Events</li>
                <li className="hover:bg-indigo-700 p-2 rounded">Settings</li>
              </ul>
            </div>
          </nav>
          
          <main className="flex-1 p-6 overflow-auto">
            {renderContent()}
          </main>
        </div>
      </div>

      {/* Add User Modal */}
      {showAddUser && (
        <div className="fixed inset-0 bg-black bg-opacity-50 flex items-center justify-center z-50">
          <div className="bg-white rounded-lg p-6 w-full max-w-md">
            <h3 className="text-xl font-bold mb-4">Add System User</h3>
            <form onSubmit={handleAddUser}>
              <div className="space-y-4">
                <div>
                  <label className="block text-sm font-medium text-gray-700">Full Name *</label>
                  <input
                    type="text"
                    required
                    className="mt-1 block w-full border border-gray-300 rounded-md px-3 py-2"
                    value={newUser.fullName}
                    onChange={(e) => setNewUser({...newUser, fullName: e.target.value})}
                  />
                </div>
                <div>
                  <label className="block text-sm font-medium text-gray-700">Email *</label>
                  <input
                    type="email"
                    required
                    className="mt-1 block w-full border border-gray-300 rounded-md px-3 py-2"
                    value={newUser.email}
                    onChange={(e) => setNewUser({...newUser, email: e.target.value})}
                  />
                </div>
                <div>
                  <label className="block text-sm font-medium text-gray-700">Password *</label>
                  <input
                    type="password"
                    required
                    className="mt-1 block w-full border border-gray-300 rounded-md px-3 py-2"
                    value={newUser.password}
                    onChange={(e) => setNewUser({...newUser, password: e.target.value})}
                  />
                </div>
                <div>
                  <label className="block text-sm font-medium text-gray-700">Role</label>
                  <select
                    className="mt-1 block w-full border border-gray-300 rounded-md px-3 py-2"
                    value={newUser.roleLevel}
                    onChange={(e) => setNewUser({...newUser, roleLevel: parseInt(e.target.value)})}
                  >
                    <option value={2}>Staff</option>
                    <option value={3}>Administrator</option>
                  </select>
                </div>
              </div>
              <div className="mt-6 flex justify-end space-x-3">
                <button
                  type="button"
                  onClick={() => setShowAddUser(false)}
                  className="px-4 py-2 text-gray-600 hover:text-gray-800"
                >
                  Cancel
                </button>
                <button
                  type="submit"
                  className="bg-purple-600 text-white px-4 py-2 rounded-md hover:bg-purple-700"
                >
                  Add User
                </button>
              </div>
            </form>
          </div>
        </div>
      )}

      {/* Add Member Modal */}
      {showAddMember && (
        <div className="fixed inset-0 bg-black bg-opacity-50 flex items-center justify-center z-50">
          <div className="bg-white rounded-lg p-6 w-full max-w-md">
            <h3 className="text-xl font-bold mb-4">Add Church Member</h3>
            <form onSubmit={handleAddMember}>
              <div className="space-y-4">
                {/* First Name and Last Name Fields */}
                <div className="grid grid-cols-2 gap-4">
                  <div>
                    <label className="block text-sm font-medium text-gray-700">First Name *</label>
                    <input
                      type="text"
                      required
                      className="mt-1 block w-full border border-gray-300 rounded-md px-3 py-2"
                      value={newMember.firstName}
                      onChange={(e) => {
                        const firstName = e.target.value;
                        setNewMember({
                          ...newMember, 
                          firstName: firstName,
                          fullName: `${firstName} ${newMember.lastName}`.trim()
                        });
                      }}
                    />
                  </div>
                  <div>
                    <label className="block text-sm font-medium text-gray-700">Last Name *</label>
                    <input
                      type="text"
                      required
                      className="mt-1 block w-full border border-gray-300 rounded-md px-3 py-2"
                      value={newMember.lastName}
                      onChange={(e) => {
                        const lastName = e.target.value;
                        setNewMember({
                          ...newMember, 
                          lastName: lastName,
                          fullName: `${newMember.firstName} ${lastName}`.trim()
                        });
                      }}
                    />
                  </div>
                </div>
                
                {/* Auto-populated Full Name */}
                <div>
                  <label className="block text-sm font-medium text-gray-700">Full Name (Auto-generated)</label>
                  <input
                    type="text"
                    className="mt-1 block w-full border border-gray-300 rounded-md px-3 py-2 bg-gray-50"
                    value={newMember.fullName}
                    readOnly
                  />
                </div>

                <div>
                  <label className="block text-sm font-medium text-gray-700">Email</label>
                  <input
                    type="email"
                    className="mt-1 block w-full border border-gray-300 rounded-md px-3 py-2"
                    value={newMember.email}
                    onChange={(e) => setNewMember({...newMember, email: e.target.value})}
                  />
                </div>

                <div>
                  <label className="block text-sm font-medium text-gray-700">Phone</label>
                  <input
                    type="tel"
                    className="mt-1 block w-full border border-gray-300 rounded-md px-3 py-2"
                    value={newMember.phoneNumber}
                    onChange={(e) => setNewMember({...newMember, phoneNumber: e.target.value})}
                  />
                </div>

                <div>
                  <label className="block text-sm font-medium text-gray-700">Status</label>
                  <select
                    className="mt-1 block w-full border border-gray-300 rounded-md px-3 py-2"
                    value={newMember.membershipStatus}
                    onChange={(e) => setNewMember({...newMember, membershipStatus: e.target.value})}
                  >
                    <option value="Active">Active</option>
                    <option value="Inactive">Inactive</option>
                    <option value="Visitor">Visitor</option>
                  </select>
                </div>

                <div>
                  <label className="block text-sm font-medium text-gray-700">Household</label>
                  <select
                    className="mt-1 block w-full border border-gray-300 rounded-md px-3 py-2"
                    value={newMember.householdId || ''}
                    onChange={(e) => setNewMember({...newMember, householdId: e.target.value || null})}
                  >
                    <option value="">No Household</option>
                    {households.map(household => (
                      <option key={household.id} value={household.id}>
                        {household.name}
                      </option>
                    ))}
                  </select>
                </div>
              </div>
              <div className="mt-6 flex justify-end space-x-3">
                <button
                  type="button"
                  onClick={() => setShowAddMember(false)}
                  className="px-4 py-2 text-gray-600 hover:text-gray-800"
                >
                  Cancel
                </button>
                <button
                  type="submit"
                  className="bg-indigo-600 text-white px-4 py-2 rounded-md hover:bg-indigo-700"
                >
                  Add Member
                </button>
              </div>
            </form>
          </div>
        </div>
      )}

      {/* Add Household Modal */}
      {showAddHousehold && (
        <div className="fixed inset-0 bg-black bg-opacity-50 flex items-center justify-center z-50">
          <div className="bg-white rounded-lg p-6 w-full max-w-md">
            <h3 className="text-xl font-bold mb-4">Add New Household</h3>
            <form onSubmit={handleAddHousehold}>
              <div className="space-y-4">
                <div>
                  <label className="block text-sm font-medium text-gray-700">Household Name *</label>
                  <input
                    type="text"
                    required
                    className="mt-1 block w-full border border-gray-300 rounded-md px-3 py-2"
                    value={newHousehold.name}
                    onChange={(e) => setNewHousehold({...newHousehold, name: e.target.value})}
                  />
                </div>
                <div>
                  <label className="block text-sm font-medium text-gray-700">Address *</label>
                  <input
                    type="text"
                    required
                    className="mt-1 block w-full border border-gray-300 rounded-md px-3 py-2"
                    value={newHousehold.address}
                    onChange={(e) => setNewHousehold({...newHousehold, address: e.target.value})}
                  />
                </div>
                <div className="grid grid-cols-2 gap-4">
                  <div>
                    <label className="block text-sm font-medium text-gray-700">City *</label>
                    <input
                      type="text"
                      required
                      className="mt-1 block w-full border border-gray-300 rounded-md px-3 py-2"
                      value={newHousehold.city}
                      onChange={(e) => setNewHousehold({...newHousehold, city: e.target.value})}
                    />
                  </div>
                  <div>
                    <label className="block text-sm font-medium text-gray-700">State *</label>
                    <input
                      type="text"
                      required
                      className="mt-1 block w-full border border-gray-300 rounded-md px-3 py-2"
                      value={newHousehold.state}
                      onChange={(e) => setNewHousehold({...newHousehold, state: e.target.value})}
                    />
                  </div>
                </div>
                <div>
                  <label className="block text-sm font-medium text-gray-700">ZIP Code *</label>
                  <input
                    type="text"
                    required
                    className="mt-1 block w-full border border-gray-300 rounded-md px-3 py-2"
                    value={newHousehold.zipCode}
                    onChange={(e) => setNewHousehold({...newHousehold, zipCode: e.target.value})}
                  />
                </div>
              </div>
              <div className="mt-6 flex justify-end space-x-3">
                <button
                  type="button"
                  onClick={() => setShowAddHousehold(false)}
                  className="px-4 py-2 text-gray-600 hover:text-gray-800"
                >
                  Cancel
                </button>
                <button
                  type="submit"
                  className="bg-green-600 text-white px-4 py-2 rounded-md hover:bg-green-700"
                >
                  Add Household
                </button>
              </div>
            </form>
          </div>
        </div>
      )}
    </div>
  );
}

export default App;