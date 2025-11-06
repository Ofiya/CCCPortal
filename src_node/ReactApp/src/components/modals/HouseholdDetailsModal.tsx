const HouseholdDetailsModal = () => {
    return (
            <div className="bg-white rounded-lg shadow-lg w-full max-w-4xl mx-4 max-h-[90vh] overflow-y-auto">
                <div className="flex justify-between items-center border-b border-gray-200 px-6 py-4">
                    <h3 className="text-xl font-semibold text-gray-800" id="household-details-title">Household Details</h3>
                    <button id="close-household-details-modal" className="invisible text-gray-500 hover:text-gray-700">
                        <i className="fas fa-times text-xl"></i>
                    </button>
                </div>
                <div className="p-6">
                    <div id="household-details-content">
                        {/* <!-- Household details will be populated here --> */}
                    </div>
                </div>
            </div>
    )
}

export default HouseholdDetailsModal