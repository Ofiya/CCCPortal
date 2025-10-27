import { Link } from "react-router"
const Sidebar = () => {
    return (
        <nav className="sidebar bg-indigo-800 text-white flex flex-col h-full sticky top-0">
            <div className="p-4 flex items-center">
                <i className="fas fa-church text-2xl mr-3"></i>
                <span className="logo-text font-semibold text-lg">Redemption Parish</span>
            </div>

            <div className="flex flex-col flex-1 overflow-y-auto">
                <Link to="/dashboard" className="sidebar-link py-3 px-4 flex items-center hover:bg-indigo-700 active:bg-indigo-900">
                    <i className="fas fa-tachometer-alt w-6 text-center"></i>
                    <span className="ml-3">Dashboard</span>
                </Link>
                <Link to="/members" className="sidebar-link py-3 px-4 flex items-center hover:bg-indigo-700">
                    <i className="fas fa-users w-6 text-center"></i>
                    <span className="ml-3">Members</span>
                </Link>
                <Link to="/household" className="sidebar-link py-3 px-4 flex items-center hover:bg-indigo-700">
                    <i className="fas fa-home w-6 text-center"></i>
                    <span className="ml-3">Households</span>
                </Link>
                <Link to="/attendance" className="sidebar-link py-3 px-4 flex items-center hover:bg-indigo-700">
                    <i className="fas fa-clipboard-check w-6 text-center"></i>
                    <span className="ml-3">Attendance</span>
                </Link>
                <Link to="/reports" className="sidebar-link py-3 px-4 flex items-center hover:bg-indigo-700 admin-only">
                    <i className="fas fa-chart-bar w-6 text-center"></i>
                    <span className="ml-3">Reports</span>
                </Link>
                <Link to="/settings" className="sidebar-link py-3 px-4 flex items-center hover:bg-indigo-700 admin-only">
                    <i className="fas fa-cog w-6 text-center"></i>
                    <span className="ml-3">Settings</span>
                </Link>
            </div>

            <div className="p-4">
                <div className="sidebar-link py-2 px-4 flex items-center text-sm opacity-75">
                    <i className="fas fa-info-circle w-6 text-center"></i>
                    <span className="ml-3">Version 2.0</span>
                </div>
            </div>
        </nav>
    )
}

export default Sidebar