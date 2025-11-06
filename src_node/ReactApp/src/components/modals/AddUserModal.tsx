const AddUserModal = () => {
    return (
            <div className="bg-white rounded-lg shadow-lg w-full max-w-md mx-4">
                <div className="flex justify-between items-center border-b border-gray-200 px-6 py-4">
                    <h3 className="text-xl font-semibold text-gray-800">Add New User</h3>
                    <button id="close-user-modal" className="invisible text-gray-500 hover:text-gray-700">
                        <i className="fas fa-times text-xl"></i>
                    </button>
                </div>
                <div className="p-6">
                    <div className="space-y-4">
                        <div>
                            <label className="block text-sm font-medium text-gray-700 mb-1">Full Name*</label>
                            <input type="text" id="user-name-input" className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-indigo-500" placeholder="Enter full name" />
                        </div>
                        <div>
                            <label className="block text-sm font-medium text-gray-700 mb-1">Email*</label>
                            <input type="email" id="user-email-input" className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-indigo-500" placeholder="Enter email" />
                        </div>
                        <div>
                            <label className="block text-sm font-medium text-gray-700 mb-1">Password*</label>
                            <input type="password" id="user-password-input" className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-indigo-500" placeholder="Enter password" />
                        </div>
                        <div>
                            <label className="block text-sm font-medium text-gray-700 mb-1">Confirm Password*</label>
                            <input type="password" id="user-confirm-password" className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-indigo-500" placeholder="Confirm password" />
                        </div>
                        <div>
                            <label className="block text-sm font-medium text-gray-700 mb-1">Role*</label>
                            <select id="user-role-select" className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-indigo-500">
                                <option value="1">View Only (Level 1)</option>
                                <option value="2">Attendance Taker (Level 2)</option>
                                <option value="3">Administrator (Level 3)</option>
                            </select>
                        </div>
                        <div className="mb-4">
                            <label className="block text-sm font-medium text-gray-700 mb-2">Permissions</label>
                            <div className="space-y-2">
                                <label className="inline-flex items-center">
                                    <input type="checkbox" className="form-checkbox h-4 w-4 text-indigo-600" checked />
                                        <span className="ml-2 text-sm text-gray-700">View Members</span>
                                </label>
                                <label className="inline-flex items-center">
                                    <input type="checkbox" className="form-checkbox h-4 w-4 text-indigo-600" checked />
                                        <span className="ml-2 text-sm text-gray-700">Manage Attendance</span>
                                </label>
                                <label className="inline-flex items-center">
                                    <input type="checkbox" className="form-checkbox h-4 w-4 text-indigo-600" />
                                        <span className="ml-2 text-sm text-gray-700">Manage Users</span>
                                </label>
                                <label className="inline-flex items-center">
                                    <input type="checkbox" className="form-checkbox h-4 w-4 text-indigo-600" />
                                        <span className="ml-2 text-sm text-gray-700">System Settings</span>
                                </label>
                            </div>
                        </div>
                    </div>
                    <div className="flex justify-end space-x-3 mt-6">
                        <button id="cancel-user" className="px-4 py-2 border border-gray-300 rounded-md text-gray-700 bg-white hover:bg-gray-50">
                            Cancel
                        </button>
                        <button id="save-user" className="px-4 py-2 bg-indigo-600 text-white rounded-md hover:bg-indigo-700">
                            Save User
                        </button>
                    </div>
                </div>
            </div>
    )
}

export default AddUserModal