const Reports = () => {
    return (
       <div id="reports-page" className="page p-6 admin-only-content">
          <div className="mb-6 flex justify-between items-center">
            <div>
              <h2 className="text-2xl font-bold text-gray-800">Reports</h2>
              <p className="text-gray-600">Generate and view church analytics and reports</p>
            </div>
          </div>

          <div className="bg-white rounded-lg shadow mb-6">
            <div className="p-4 flex flex-wrap gap-4 items-end">
              <div>
                <label className="block text-sm font-medium text-gray-700 mb-1">Start Date</label>
                <input id="report-start-date" type="text" className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-indigo-500" value="" />
              </div>
              
              <div>
                <label className="block text-sm font-medium text-gray-700 mb-1">End Date</label>
                <input id="report-end-date" type="text" className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-indigo-500" value="" />
              </div>

              <div>
                <label className="block text-sm font-medium text-gray-700 mb-1">Report Type</label>
                <select id="report-type" className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-indigo-500">
                  <option value="attendance">Attendance Report</option>
                  <option value="members">Members Report</option>
                  <option value="households">Households Report</option>
                  <option value="flagged">Flagged Members Report</option>
                  <option value="immigration">Immigration Documents Report</option>
                  <option value="birthdays">Birthday Report</option>
                </select>
              </div>

              <div className="flex space-x-2">
                <button id="generate-report" className="bg-indigo-600 hover:bg-indigo-700 text-white font-medium py-2 px-4 rounded-md">
                  Generate Report
                </button>
                <button id="export-csv" className="bg-green-600 hover:bg-green-700 text-white font-medium py-2 px-4 rounded-md flex items-center">
                  <i className="fas fa-file-csv mr-2"></i> CSV
                </button>
                <button id="export-pdf" className="bg-red-600 hover:bg-red-700 text-white font-medium py-2 px-4 rounded-md flex items-center">
                  <i className="fas fa-file-pdf mr-2"></i> PDF
                </button>
              </div>
            </div>
          </div>

          <div className="grid grid-cols-1 lg:grid-cols-2 gap-6 mb-6">
            {/* <!-- Attendance Chart --> */}
            <div className="bg-white rounded-lg shadow p-6">
              <h3 className="text-lg font-semibold text-gray-800 mb-4">Attendance Percentage</h3>
              <div className="h-64">
                <canvas id="attendance-percentage-chart"></canvas>
              </div>
            </div>

            {/* <!-- Demographic Chart --> */}
            <div className="bg-white rounded-lg shadow p-6">
              <h3 className="text-lg font-semibold text-gray-800 mb-4">Member Demographics</h3>
              <div className="h-64">
                <canvas id="demographics-chart"></canvas>
              </div>
            </div>
          </div>

          <div className="bg-white rounded-lg shadow p-6 mb-6">
            <div className="flex justify-between items-center mb-4">
              <h3 className="text-lg font-semibold text-gray-800">Report Results</h3>
              <div className="flex space-x-2">
                <button id="export-excel-results" className="bg-green-600 hover:bg-green-700 text-white py-2 px-3 rounded-md text-sm flex items-center">
                  <i className="fas fa-file-excel mr-2"></i> Export Excel
                </button>
                <button id="export-pdf-results" className="bg-red-600 hover:bg-red-700 text-white py-2 px-3 rounded-md text-sm flex items-center">
                  <i className="fas fa-file-pdf mr-2"></i> Export PDF
                </button>
              </div>
            </div>
            <div className="overflow-x-auto">
              <table className="min-w-full" id="report-results-table">
                <thead>
                  <tr className="bg-gray-50 border-b border-gray-200">
                    {/* <!-- Table headers will be generated dynamically --> */}
                  </tr>
                </thead>
                <tbody id="report-results-body">
                  {/* <!-- Report results will be populated here --> */}
                </tbody>
              </table>
            </div>
          </div>
        </div>
    )
}

export default Reports