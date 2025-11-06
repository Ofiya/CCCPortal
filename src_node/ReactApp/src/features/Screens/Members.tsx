import { useState } from "react";
// import { useNavigate } from "react-router";
import CSVModal from "../../components/modals/CSVModal";
import AppDialog from "../../components/modals/AppDialog";
import MemberModal from "../../components/modals/MemberModal";
import { useAxios } from "../../utils/hooks/useAxios";

const Members = () => {

    const [open, setOpen] = useState(false);
    const [mamberOpen, setMemberOpen] = useState(false);

        const { data, loading, error } = useAxios("members");
        console.log(data, loading, error)


    return (
        <div id="members-page" className="page p-6">
            <div className="mb-6 flex justify-between items-center">
                <div>
                    <h2 className="text-2xl font-bold text-gray-800">Church Members</h2>
                    <p className="text-gray-600">Manage and view all church members</p>
                </div>
                <div className="flex space-x-3">
                    <button id="upload-csv-btn" onClick={() => setOpen(true)} className="bg-green-600 hover:bg-green-700 text-white font-medium py-2 px-4 rounded-md flex items-center">
                        <i className="fas fa-file-csv mr-2"></i> Upload CSV
                    </button>
                    <button id="add-member-btn" onClick={() => setMemberOpen(true)} className="bg-indigo-600 hover:bg-indigo-700 text-white font-medium py-2 px-4 rounded-md flex items-center">
                        <i className="fas fa-plus mr-2"></i> Add Member
                    </button>
                </div>
            </div>

            {/* <!-- Filters Section --> */}
            <div className="bg-white rounded-lg shadow mb-6">
                <div className="p-4 flex flex-wrap gap-4">
                    <div className="flex-1 min-w-[200px]">
                        <label className="block text-sm font-medium text-gray-700 mb-1">Search</label>
                        <div className="relative">
                            <input type="text" id="member-search" placeholder="Search members..." className="w-full pl-10 pr-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-indigo-500" />
                            <div className="absolute inset-y-0 left-0 flex items-center pl-3">
                                <i className="fas fa-search text-gray-400"></i>
                            </div>
                        </div>
                    </div>
                    <div>
                        <label className="block text-sm font-medium text-gray-700 mb-1">Rows per page</label>
                        <select id="rows-per-page" className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-indigo-500">
                            <option value="10" selected>10</option>
                            <option value="20">20</option>
                            <option value="50">50</option>
                            <option value="100">100</option>
                        </select>
                    </div>
                    <div>
                        <label className="block text-sm font-medium text-gray-700 mb-1">Gender</label>
                        <select id="gender-filter" className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-indigo-500">
                            <option value="">All Genders</option>
                            <option value="Male">Male</option>
                            <option value="Female">Female</option>
                        </select>
                    </div>

                    <div>
                        <label className="block text-sm font-medium text-gray-700 mb-1">Household</label>
                        <select id="household-filter" className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-indigo-500">
                            <option value="">All Households</option>
                            {/* <!-- Will be populated dynamically --> */}
                        </select>
                    </div>
                </div>
            </div>

            {/* <!-- Members Table --> */}
            <div className="bg-white rounded-lg shadow overflow-hidden">
                <div className="overflow-x-auto">
                    <table className="min-w-full">
                        <thead>
                            <tr className="bg-gray-50 border-b border-gray-200">
                                <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">Member</th>
                                <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">Gender</th>
                                <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">Date of Birth</th>
                                <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">Household</th>
                                <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">Contact</th>
                                <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">Immigration Status</th>
                                <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">Actions</th>
                            </tr>
                        </thead>
                        <tbody id="members-table-body" className="bg-white divide-y divide-gray-200">
                            {/* <!-- Members will be populated dynamically --> */}
                        </tbody>
                    </table>
                </div>
                <div className="px-4 py-3 border-t border-gray-200">
                    <div className="flex items-center justify-between">
                        <div className="text-sm text-gray-700">
                            Showing <span className="font-medium" id="members-visible-count">0</span> of <span className="font-medium" id="members-total-count">0</span> members
                        </div>
                        <div className="flex space-x-1" id="members-pagination">
                            {/* <!-- Pagination will be generated here --> */}
                        </div>
                    </div>
                </div>
            </div>

            {/* <!-- Add Member Floating Button (Mobile) --> */}
            <button id="add-member-floating-btn" className="floating-button md:hidden fixed bottom-6 right-6 w-14 h-14 bg-indigo-600 text-white rounded-full shadow-lg flex items-center justify-center">
                <i className="fas fa-plus text-xl"></i>
            </button>
            <AppDialog
                isOpen={open}
                onClose={() => setOpen(false)}
            >

                <CSVModal />
            </AppDialog>
            <AppDialog
                isOpen={mamberOpen}
                onClose={() => setMemberOpen(false)}
            >

                <MemberModal />
            </AppDialog>
        </div>
    )
}

export default Members