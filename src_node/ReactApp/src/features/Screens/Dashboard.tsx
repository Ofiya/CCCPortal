// @ts-nocheck
import { useNavigate } from "react-router"
import { useAxios } from "../../utils/hooks/useAxios";
import Loading from "../../components/Layout/Loader/Loading";
import BarChart from "../../components/Charts/Barchart";
// import "../../utils/main"

const Dashboard = () => {




    const navigate = useNavigate()
    const { data, loading, error } = useAxios("Dashboard/stats");
    const { data:BirthDayData, loading:BirthDayLoading, error:BirthDayError } = useAxios("Dashboard/birthdays");
    const { data:WelfareData, loading:WelfareLoading, error:WelfareError } = useAxios("Dashboard/welfare");
    console.log(data, loading, error)

    if (loading) {
        return (
            <div className="h-screen flex items-center justify-center">
                <Loading />
            </div>
        )
    } else if (error) {
        return (
            <div>An Error Occured</div>
        )
    }

    return (
        <div id="dashboard-page" className="page p-6">
            <div className="mb-6">
                <h2 className="text-2xl font-bold text-gray-800">Dashboard</h2>
                <p className="text-gray-600">Overview of CCC Redemption Parish</p>
            </div>

            <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-6 mb-8">
                {/* <!-- Total Members Card --> */}
                <div className="bg-white rounded-lg shadow p-6 cursor-pointer hover:shadow-md transition-shadow" onClick={() => navigate('/members')}>
                    <div className="flex items-center">
                        <div className="rounded-full bg-blue-100 p-3">
                            <i className="fas fa-users text-blue-600"></i>
                        </div>
                        <div className="ml-4">
                            <h3 className="text-sm font-medium text-gray-500">Total Members</h3>
                            <p className="text-2xl font-semibold text-gray-800" id="total-members">{data?.totalMembers}</p>
                        </div>
                    </div>
                </div>

                {/* <!-- Attendance Rate Card --> */}
                <div className="bg-white rounded-lg shadow p-6 cursor-pointer hover:shadow-md transition-shadow" onClick={() => navigate('/attendance')}>
                    <div className="flex items-center">
                        <div className="rounded-full bg-green-100 p-3">
                            <i className="fas fa-chart-line text-green-600"></i>
                        </div>
                        <div className="ml-4">
                            <h3 className="text-sm font-medium text-gray-500">Attendance Rate</h3>
                            <p className="text-2xl font-semibold text-gray-800" id="attendance-rate">{data?.attendanceRate}%</p>
                        </div>
                    </div>
                </div>

                {/* <!-- Flagged Members Card --> */}
                <div className="bg-white rounded-lg shadow p-6 cursor-pointer hover:shadow-md transition-shadow" onClick={() => navigate('/members')}>
                    <div className="flex items-center">
                        <div className="rounded-full bg-red-100 p-3">
                            <i className="fas fa-flag text-red-600"></i>
                        </div>
                        <div className="ml-4">
                            <h3 className="text-sm font-medium text-gray-500">Flagged Members</h3>
                            <p className="text-2xl font-semibold text-gray-800" id="flagged-members">{data?.flaggedMembers}</p>
                        </div>
                    </div>
                </div>

                {/* <!-- Expiring Documents Card --> */}
                <div className="bg-white rounded-lg shadow p-6 cursor-pointer hover:shadow-md transition-shadow" onClick={() => navigate('/members')}>
                    <div className="flex items-center">
                        <div className="rounded-full bg-yellow-100 p-3">
                            <i className="fas fa-exclamation-triangle text-yellow-600"></i>
                        </div>
                        <div className="ml-4">
                            <h3 className="text-sm font-medium text-gray-500">Expiring Documents</h3>
                            <p className="text-2xl font-semibold text-gray-800" id="expiring-documents">{data?.expiringDocuments}</p>
                        </div>
                    </div>
                </div>
            </div>

            <div className="grid grid-cols-1 lg:grid-cols-2 gap-6">
                {/* <!-- Enhanced Attendance Chart --> */}
                <div className="bg-white rounded-lg shadow p-6">
                    <h3 className="text-lg font-semibold text-gray-800 mb-4">Attendance Analytics</h3>
                    <div className="h-64">
                        <BarChart />
                    </div>
                </div>

                {/* <!-- Upcoming Birthdays --> */}
                <div className="bg-white rounded-lg shadow p-6">
                    <h3 className="text-lg font-semibold text-gray-800 mb-4">Upcoming Birthdays</h3>
                    <div className="overflow-y-auto max-h-64">
                        <table className="min-w-full">
                            <thead>
                                <tr className="border-b border-gray-200">
                                    <th className="py-2 text-left text-sm font-medium text-gray-500">Name</th>
                                    <th className="py-2 text-left text-sm font-medium text-gray-500">Date</th>
                                    <th className="py-2 text-left text-sm font-medium text-gray-500">Days</th>
                                    <th className="py-2 text-left text-sm font-medium text-gray-500">Actions</th>
                                </tr>
                            </thead>
                            <tbody id="birthdays-table-body">
                                {/* <!-- Will be populated dynamically --> */}
                            </tbody>
                        </table>
                    </div>
                </div>

                {/* <!-- Welfare Dashboard --> */}
                <div className="bg-white rounded-lg shadow p-6">
                    <h3 className="text-lg font-semibold text-gray-800 mb-4">Welfare Unit Dashboard</h3>
                    <div className="grid grid-cols-2 gap-4 mb-4">
                        <div className="bg-blue-50 p-4 rounded-lg">
                            <div className="text-blue-600 font-semibold text-sm">Members Needing Follow-up</div>
                            <div className="text-2xl font-bold text-blue-800" id="follow-up-count">0</div>
                        </div>
                        <div className="bg-green-50 p-4 rounded-lg">
                            <div className="text-green-600 font-semibold text-sm">Recent Follow-ups</div>
                            <div className="text-2xl font-bold text-green-800" id="recent-followups">0</div>
                        </div>
                    </div>
                    <div className="overflow-y-auto max-h-40">
                        <table className="min-w-full">
                            <thead>
                                <tr className="border-b border-gray-200">
                                    <th className="py-2 text-left text-sm font-medium text-gray-500">Member</th>
                                    <th className="py-2 text-left text-sm font-medium text-gray-500">Last Service</th>
                                    <th className="py-2 text-left text-sm font-medium text-gray-500">Status</th>
                                </tr>
                            </thead>
                            <tbody id="welfare-table-body">
                                {/* <!-- Will be populated dynamically --> */}
                            </tbody>
                        </table>
                    </div>
                </div>

                {/* <!-- Member Demographics --> */}
                <div className="bg-white rounded-lg shadow p-6">
                    <h3 className="text-lg font-semibold text-gray-800 mb-4">Member Demographics</h3>
                    <div className="h-64">
                        <canvas id="demographics-chart"></canvas>
                    </div>
                </div>
            </div>
        </div>
    )
}

export default Dashboard