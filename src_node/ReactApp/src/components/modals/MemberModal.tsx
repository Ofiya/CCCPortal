const MemberModal = () => {
    return (
            <div className="bg-white rounded-lg shadow-lg w-full max-w-4xl mx-4 max-h-[90vh] overflow-y-auto">
                <div className="flex justify-between items-center border-b border-gray-200 px-6 py-4">
                    <h3 className="text-xl font-semibold text-gray-800">Add New Member</h3>
                    <button id="close-member-modal" className="invisible text-gray-500 hover:text-gray-700">
                        <i className="fas fa-times text-xl"></i>
                    </button>
                </div>
                <div className="p-6">
                    <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
                        <div className="md:col-span-2 flex items-center space-x-4">
                            <div className="h-24 w-24 bg-gray-200 rounded-full flex items-center justify-center relative">
                                <i className="fas fa-user text-gray-400 text-4xl"></i>
                                <input type="file" id="profile-photo" className="hidden" />
                                    <label htmlFor="profile-photo" className="absolute bottom-0 right-0 bg-indigo-600 text-white p-2 rounded-full cursor-pointer">
                                        <i className="fas fa-camera"></i>
                                    </label>
                            </div>
                            <div>
                                <p className="text-sm text-gray-600">Upload a profile photo</p>
                                <p className="text-xs text-gray-500">JPG or PNG format, max 5MB</p>
                            </div>
                        </div>

                        <div>
                            <label className="block text-sm font-medium text-gray-700 mb-1">First Name*</label>
                            <input type="text" id="first-name" className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-indigo-500" placeholder="Enter first name" />
                        </div>

                        <div>
                            <label className="block text-sm font-medium text-gray-700 mb-1">Last Name*</label>
                            <input type="text" id="last-name" className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-indigo-500" placeholder="Enter last name" />
                        </div>

                        <div className="md:col-span-2">
                            <label className="block text-sm font-medium text-gray-700 mb-1">Full Name*</label>
                            <input type="text" id="full-name" className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-indigo-500" placeholder="Auto-populated" readOnly />
                        </div>

                        <div>
                            <label className="block text-sm font-medium text-gray-700 mb-1">Gender*</label>
                            <select className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-indigo-500">
                                <option value="">Select gender</option>
                                <option value="Male">Male</option>
                                <option value="Female">Female</option>
                            </select>
                        </div>

                        <div>
                            <label className="block text-sm font-medium text-gray-700 mb-1">Date of Birth*</label>
                            <input type="text" id="dob" className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-indigo-500" placeholder="Select date" />
                        </div>

                        <div>
                            <label className="block text-sm font-medium text-gray-700 mb-1">Occupation</label>
                            <input type="text" className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-indigo-500" placeholder="Occupation" />
                        </div>

                        <div>
                            <label className="block text-sm font-medium text-gray-700 mb-1">Marital Status</label>
                            <select className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-indigo-500">
                                <option value="">Select status</option>
                                <option value="Single">Single</option>
                                <option value="Married">Married</option>
                                <option value="Divorced">Divorced</option>
                                <option value="Widowed">Widowed</option>
                            </select>
                        </div>

                        <div>
                            <label className="block text-sm font-medium text-gray-700 mb-1">Phone Number*</label>
                            <input type="tel" className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-indigo-500" placeholder="Enter phone number" />
                        </div>

                        <div>
                            <label className="block text-sm font-medium text-gray-700 mb-1">Email</label>
                            <input type="email" className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-indigo-500" placeholder="Enter email address" />
                        </div>

                        <div className="md:col-span-2">
                            <label className="block text-sm font-medium text-gray-700 mb-1">Address</label>
                            <input type="text" className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-indigo-500" placeholder="Enter address" />
                        </div>

                        <div>
                            <label className="block text-sm font-medium text-gray-700 mb-1">Household</label>
                            <select className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-indigo-500">
                                <option value="">Select household</option>
                                {/* <!-- Will be populated dynamically --> */}
                                <option value="new">Create New Household</option>
                            </select>
                        </div>

                        <div>
                            <label className="block text-sm font-medium text-gray-700 mb-1">Rank in Church</label>
                            <select className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-indigo-500">
                                <option value="">Select rank</option>
                                <option value="Member">Member</option>
                                <option value="Deacon">Deacon</option>
                                <option value="Elder">Elder</option>
                                <option value="Pastor">Pastor</option>
                            </select>
                        </div>

                        <div>
                            <label className="block text-sm font-medium text-gray-700 mb-1">Immigration Status</label>
                            <select className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-indigo-500">
                                <option value="">Select status</option>
                                <option value="Citizen">Citizen</option>
                                <option value="Permanent Resident">Permanent Resident</option>
                                <option value="Work Permit">Work Permit</option>
                                <option value="Study Permit">Study Permit</option>
                                <option value="Visitor">Visitor</option>
                                <option value="Refugee">Refugee</option>
                            </select>
                        </div>

                        <div>
                            <label className="block text-sm font-medium text-gray-700 mb-1">Immigration Document Expiry</label>
                            <input type="text" id="document-expiry" className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-indigo-500" placeholder="Select date" />
                        </div>

                        <div>
                            <label className="block text-sm font-medium text-gray-700 mb-1">Date Joined</label>
                            <input type="text" id="date-joined" className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-indigo-500" placeholder="Select date" />
                        </div>

                        <div>
                            <label className="block text-sm font-medium text-gray-700 mb-1">Assigned Welfare Member</label>
                            <select className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-indigo-500">
                                <option value="">Select member</option>
                                {/* <!-- Will be populated dynamically --> */}
                            </select>
                        </div>

                        <div className="md:col-span-2">
                            <label className="block text-sm font-medium text-gray-700 mb-1">Additional Notes</label>
                            <textarea className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-indigo-500" rows={3} placeholder="Enter any additional information..."></textarea>
                        </div>
                    </div>

                    <div className="flex justify-end space-x-3 mt-6">
                        <button id="cancel-member" className="px-4 py-2 border border-gray-300 rounded-md text-gray-700 bg-white hover:bg-gray-50">
                            Cancel
                        </button>
                        <button id="save-member" className="px-4 py-2 bg-indigo-600 text-white rounded-md hover:bg-indigo-700">
                            Save Member
                        </button>
                    </div>
                </div>
            </div>
    )
}

export default MemberModal