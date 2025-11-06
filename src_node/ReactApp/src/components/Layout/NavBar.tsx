const NavBar = () => {
    return (
        <header className="sticky top-0 bg-white shadow-sm z-10">
            <div className="flex justify-between items-center px-4 py-3">
                <div className="flex items-center">
                    <button id="sidebar-toggle" className="mr-3 text-gray-600 hover:text-indigo-600 focus:outline-none">
                        <i className="fas fa-bars text-xl"></i>
                    </button>
                    <h1 className="text-xl font-semibold text-gray-800">CCC Redemption Parish MMP</h1>
                </div>
                <div className="flex items-center space-x-4">
                    <div className="dropdown">
                        <button id="user-menu-button" className="flex items-center focus:outline-none">
                            <span className="mr-2 text-sm font-medium text-gray-700 hidden sm:block" id="user-name">Admin User</span>
                            <div className="h-8 w-8 rounded-full bg-indigo-600 flex items-center justify-center text-white font-semibold">
                                A
                            </div>
                        </button>
                        <div className="dropdown-menu bg-white shadow-lg rounded-md py-1">
                            <a href="javascript:void(0)" className="block px-4 py-2 text-sm text-gray-700 hover:bg-gray-100">Profile</a>
                            <a href="javascript:void(0)" className="block px-4 py-2 text-sm text-gray-700 hover:bg-gray-100">Settings</a>
                            <a id="logout-button" href="javascript:void(0)" className="block px-4 py-2 text-sm text-gray-700 hover:bg-gray-100">Logout</a>
                        </div>
                    </div>
                </div>
            </div>
        </header>
    )
}

export default NavBar