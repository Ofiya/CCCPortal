const CSVModal = () => {
    return (
        <div className="relative bg-white rounded-lg shadow-lg w-full max-w-md mx-auto">
            <div className="flex justify-between items-center border-b border-gray-200 px-6 py-4">
                <h3 className="text-xl font-semibold text-gray-800">Upload Member CSV</h3>
                <button id="close-csv-modal" className="invisible text-gray-500 hover:text-gray-700">
                    <i className="fas fa-times text-xl"></i>
                </button>
            </div>
            <div className="p-6">
                <div className="mb-4">
                    <p className="text-sm text-gray-600 mb-2">Upload a CSV file with member details. Maximum file size: 25MB.</p>
                    <p className="text-xs text-gray-500">Required columns: First Name, Last Name, Gender</p>
                    <p className="text-xs text-gray-500">Optional columns: Phone Number, Email, Date of Birth, Address</p>
                </div>
                <div className="mb-4">
                    <label className="block text-sm font-medium text-gray-700 mb-2">Select CSV File

                    <div className="border-2 border-dashed border-gray-300 rounded-md p-4 text-center hover:border-indigo-400 transition-colors cursor-pointer" id="csv-drop-zone">
                        <input type="file" id="csv-file-input" accept=".csv" className="hidden" />
                        <div className="flex flex-col items-center justify-center">
                            <i className="fas fa-cloud-upload-alt text-3xl text-gray-400 mb-2"></i>
                            <p className="text-sm text-gray-600">Click to upload or drag and drop</p>
                            <p className="text-xs text-gray-500 mt-1">CSV files only (max. 25MB)</p>
                        </div>
                    </div>
                    <div id="file-name" className="text-sm text-gray-700 mt-2 hidden"></div>
                    </label>
                </div>
                <div id="upload-progress" className="hidden mb-4">
                    <div className="flex justify-between text-sm text-gray-600 mb-1">
                        <span>Uploading...</span>
                        <span id="progress-percentage">0%</span>
                    </div>
                    <div className="w-full bg-gray-200 rounded-full h-2.5">
                        <div id="progress-bar" className="bg-indigo-600 h-2.5 rounded-full transition-all duration-300" style={{ "width": "0%" }}></div>
                    </div>
                    <p className="text-xs text-gray-500 mt-1 text-center" id="upload-status">Processing...</p>
                </div>
                <div id="upload-success" className="hidden mb-4 p-3 bg-green-50 border border-green-200 rounded-md">
                    <div className="flex items-center">
                        <i className="fas fa-check-circle text-green-500 mr-2"></i>
                        <p className="text-sm text-green-700">File uploaded successfully!</p>
                    </div>
                </div>
                <div id="upload-error" className="hidden mb-4 p-3 bg-red-50 border border-red-200 rounded-md">
                    <div className="flex items-center">
                        <i className="fas fa-exclamation-circle text-red-500 mr-2"></i>
                        <p className="text-sm text-red-700" id="error-message">Error uploading file</p>
                    </div>
                </div>
                <div className="flex justify-end space-x-3">
                    <button id="cancel-upload" className="px-4 py-2 border border-gray-300 rounded-md text-gray-700 bg-white hover:bg-gray-50 transition-colors">Cancel</button>
                    <button id="start-upload" className="px-4 py-2 bg-indigo-600 text-white rounded-md hover:bg-indigo-700 transition-colors disabled:opacity-50 disabled:cursor-not-allowed" disabled>
                        <i className="fas fa-upload mr-2"></i>Upload
                    </button>
                </div>
            </div>
        </div>
    )
}

export default CSVModal