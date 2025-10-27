const Household = () => {
    return (
        <div id="households-page" className="page p-6">
            <div className="mb-6 flex justify-between items-center">
                <div>
                    <h2 className="text-2xl font-bold text-gray-800">Households</h2>
                    <p className="text-gray-600">Manage church households and family units</p>
                </div>
                <button id="add-household-btn" className="bg-indigo-600 hover:bg-indigo-700 text-white font-medium py-2 px-4 rounded-md flex items-center">
                    <i className="fas fa-plus mr-2"></i> Add Household
                </button>
            </div>

            <div className="bg-white rounded-lg shadow mb-6">
                <div className="p-4 flex flex-wrap gap-4">
                    <div className="flex-1 min-w-[200px]">
                        <label className="block text-sm font-medium text-gray-700 mb-1">Search</label>
                        <div className="relative">
                            <input type="text" id="household-search" placeholder="Search households..." className="w-full pl-10 pr-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-indigo-500" />
                                <div className="absolute inset-y-0 left-0 flex items-center pl-3">
                                    <i className="fas fa-search text-gray-400"></i>
                                </div>
                        </div>
                    </div>
                </div>
            </div>

            <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6" id="households-container">
                {/* <!-- Household cards will be populated dynamically --> */}
            </div>

            {/* <!-- Pagination for Households --> */}
            <div className="mt-6 bg-white rounded-lg shadow px-4 py-3">
                <div className="flex items-center justify-between">
                    <div className="text-sm text-gray-700">
                        Showing <span className="font-medium" id="households-visible-count">0</span> of <span className="font-medium" id="households-total-count">0</span> households
                    </div>
                    <div className="flex space-x-1" id="households-pagination">
                        {/* <!-- Pagination will be generated here --> */}
                    </div>
                </div>
            </div>

            {/* <!-- Add Household Floating Button (Mobile) --> */}
            <button id="add-household-floating-btn" className="floating-button md:hidden fixed bottom-6 right-6 w-14 h-14 bg-indigo-600 text-white rounded-full shadow-lg flex items-center justify-center">
                <i className="fas fa-plus text-xl"></i>
            </button>
        </div>
    )
}

export default Household