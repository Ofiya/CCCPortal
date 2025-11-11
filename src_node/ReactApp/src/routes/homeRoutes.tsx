import Dashboard from "../features/Screens/Dashboard";
import Members from "../features/Screens/Members";
import Household from "../features/Screens/Household";
import Attendance from "../features/Screens/Attendance";
import Reports from "../features/Screens/Reports";
import Settings from "../features/Screens/Settings";
import { Navigate } from "react-router";


export const homeRoutes = [
    { path: "/", element: <Navigate to="/auth" /> },
    { path: "/dashboard", element: <Dashboard /> },
    { path: "/members", element: <Members /> },
    { path: "/household", element: <Household /> },
    { path: "/attendance", element: <Attendance /> },
    { path: "/reports", element: <Reports /> },
    { path: "/settings", element: <Settings /> },

]