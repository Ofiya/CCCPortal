const HouseHoldModal = () => {
    return (
        <div className="bg-white rounded-lg shadow-lg w-full max-w-2xl mx-4">
            <div className="flex justify-between items-center border-b border-gray-200 px-6 py-4">
                <h3 className="text-xl font-semibold text-gray-800">Add New Household</h3>
                <button id="close-household-modal" className="invisible text-gray-500 hover:text-gray-700">
                    <i className="fas fa-times text-xl"></i>
                </button>
            </div>
            <div className="p-6">
                <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
                    <div>
                        <label className="block text-sm font-medium text-gray-700 mb-1">Household Name*</label>
                        <input type="text" className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-indigo-500" placeholder="e.g., Smith Family" />
                    </div>

                    <div>
                        <label className="block text-sm font-medium text-gray-700 mb-1">Head of Household*</label>
                        <select className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-indigo-500">
                            <option value="">Select member</option>
                            <option value="new">Add New Member</option>
                            {/* <!-- Will be populated dynamically --> */}
                        </select>
                    </div>

                    <div className="md:col-span-2">
                        <label className="block text-sm font-medium text-gray-700 mb-1">Address*</label>
                        <input type="text" className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-indigo-500" placeholder="Enter household address" />
                    </div>

                    <div>
                        <label className="block text-sm font-medium text-gray-700 mb-1">Primary Phone*</label>
                        <input type="tel" className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-indigo-500" placeholder="Enter phone number" />
                    </div>

                    <div>
                        <label className="block text-sm font-medium text-gray-700 mb-1">Email</label>
                        <input type="email" className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-indigo-500" placeholder="Enter email address" />
                    </div>

                    <div className="md:col-span-2">
                        <label className="block text-sm font-medium text-gray-700 mb-1">Household Members</label>
                        <div className="border border-gray-300 rounded-md p-3 max-h-40 overflow-y-auto" id="household-members-list">
                            <div className="text-sm text-gray-500 italic mb-2">Head of household will be automatically added</div>
                            {/* <!-- Will be populated dynamically --> */}
                        </div>
                    </div>

                    <div className="md:col-span-2">
                        <label className="block text-sm font-medium text-gray-700 mb-1">Notes</label>
                        <textarea className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-indigo-500" rows={3} placeholder="Enter any additional information..."></textarea>
                    </div>
                </div>

                <div className="flex justify-end space-x-3 mt-6">
                    <button id="cancel-household" className="px-4 py-2 border border-gray-300 rounded-md text-gray-700 bg-white hover:bg-gray-50">
                        Cancel
                    </button>
                    <button id="save-household" className="px-4 py-2 bg-indigo-600 text-white rounded-md hover:bg-indigo-700">
                        Save Household
                    </button>
                </div>
            </div>
        </div>
    )
}

export default HouseHoldModal