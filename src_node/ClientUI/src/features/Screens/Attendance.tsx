const Attendance = () => {
    return (
        <div id="attendance-page" className="page p-6">
            <div className="mb-6">
                <h2 className="text-2xl font-bold text-gray-800">Attendance Tracking</h2>
                <p className="text-gray-600">Record and manage Sunday service attendance</p>
            </div>

            <div className="bg-white rounded-lg shadow mb-6">
                <div className="p-4 flex flex-wrap gap-4 items-end">
                    <div>
                        <label className="block text-sm font-medium text-gray-700 mb-1">Service Date</label>
                        <input id="service-date" type="text" className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-indigo-500" value="" />
                    </div>

                    <div className="flex-1 min-w-[200px]">
                        <label className="block text-sm font-medium text-gray-700 mb-1">Search Members</label>
                        <div className="relative">
                            <input type="text" id="attendance-search" placeholder="Search by name..." className="w-full pl-10 pr-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-indigo-500" />
                                <div className="absolute inset-y-0 left-0 flex items-center pl-3">
                                    <i className="fas fa-search text-gray-400"></i>
                                </div>
                        </div>
                    </div>

                    <div>
                        <label className="block text-sm font-medium text-gray-700 mb-1">Rows per page</label>
                        <select id="attendance-rows-per-page" className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-indigo-500">
                            <option value="5">5</option>
                            <option value="10" selected>10</option>
                            <option value="20">20</option>
                            <option value="50">50</option>
                            <option value="100">100</option>
                        </select>
                    </div>

                    <div className="flex items-center space-x-4">
                        <button id="bulk-select-btn" className="bg-blue-600 hover:bg-blue-700 text-white font-medium py-2 px-4 rounded-md flex items-center">
                            <i className="fas fa-check-square mr-2"></i> Bulk Select
                        </button>
                        <button id="mark-all-present" className="bg-indigo-600 hover:bg-indigo-700 text-white font-medium py-2 px-4 rounded-md flex items-center">
                            <i className="fas fa-check-double mr-2"></i> Mark All Present
                        </button>
                        <button id="save-attendance" className="bg-green-600 hover:bg-green-700 text-white font-medium py-2 px-4 rounded-md flex items-center">
                            <i className="fas fa-save mr-2"></i> Save All
                        </button>

                        <div className="flex items-center space-x-2">
                            <div id="autosave-indicator" className="autosave-indicator text-sm flex items-center">
                                <i className="fas fa-check-circle text-green-500 mr-1"></i>
                                <span>Saved</span>
                            </div>
                        </div>
                    </div>
                </div>
            </div>

            <div className="bg-white rounded-lg shadow overflow-hidden">
                <div className="overflow-x-auto">
                    <table className="min-w-full">
                        <thead>
                            <tr className="bg-gray-50 border-b border-gray-200">
                                <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                                    <input type="checkbox" id="select-all-attendance" className="rounded border-gray-300 text-indigo-600 focus:ring-indigo-500" />
                                </th>
                                <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">Member</th>
                                <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">Gender</th>
                                <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">Phone</th>
                                <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">Attendance</th>
                                <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">Flagged for Follow-up</th>
                                <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">Notes</th>
                            </tr>
                        </thead>
                        <tbody id="attendance-table-body" className="bg-white divide-y divide-gray-200">
                            {/* <!-- Attendance rows will be populated here --> */}
                        </tbody>
                    </table>
                </div>
                <div className="px-4 py-3 border-t border-gray-200">
                    <div className="flex items-center justify-between">
                        <div className="text-sm text-gray-700">
                            Showing <span className="font-medium" id="attendance-visible-count">0</span> of <span className="font-medium" id="attendance-total-count">0</span> members
                            <span id="search-results-info" className="ml-2 text-indigo-600 hidden"></span>
                        </div>
                        <div className="flex space-x-1" id="attendance-pagination">
                            {/* <!-- Pagination buttons will be generated here --> */}
                        </div>
                    </div>
                </div>
            </div>
        </div>
    )
}

export default Attendance