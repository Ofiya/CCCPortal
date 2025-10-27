import type { ReactNode } from 'react'
import NavBar from './NavBar'
import Sidebar from './Sidebar'

const Layout = ({ children }: { children: ReactNode }) => {
    return (
        <div id="app" className="h-screen flex flex-col">
            
            <NavBar />
            <div className="flex flex-1 overflow-hidden">
                <Sidebar />
                <main className="content-area flex-1 overflow-y-scroll">
                    {children}
                </main>
            </div>
        </div>
    )
}

export default Layout