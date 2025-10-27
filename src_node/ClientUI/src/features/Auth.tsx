import React from "react";
import { useNavigate } from "react-router";

const Auth: React.FC = () => {
    const navigate = useNavigate();
    const handleSubmit = (e: React.FormEvent<HTMLFormElement>) => {
        e.preventDefault();
        // TODO: add real auth logic here
        navigate("/dashboard");
    }
    return (
        <div id="login-page" className="min-h-screen flex items-center justify-center bg-gray-100">
            <form onSubmit={handleSubmit} className="bg-white rounded-lg shadow-lg p-8 max-w-md w-full">
                <div className="text-center mb-8">
                    <h1 className="text-3xl font-bold text-indigo-700">CCC Redemption Parish</h1>
                    <p className="text-gray-600">Membership Management Platform</p>
                </div>

                <div className="mb-6">
                    <label className="block text-gray-700 text-sm font-semibold mb-2" htmlFor="email">Email Address</label>
                    <input id="email" type="email" className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-indigo-500" placeholder="Enter your email" />
                </div>

                <div className="mb-6">
                    <label className="block text-gray-700 text-sm font-semibold mb-2" htmlFor="password">Password</label>
                    <input id="password" type="password" className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-indigo-500" placeholder="Enter your password" />
                </div>

                <button id="login-button" className="w-full bg-indigo-600 text-white font-semibold py-2 px-4 rounded-md hover:bg-indigo-700 focus:outline-none focus:ring-2 focus:ring-indigo-500 focus:ring-offset-2">
                    Login
                </button>

                <div className="mt-4 text-center text-sm text-gray-600">
                    <a href="#" onClick={(e) => e.preventDefault()} className="text-indigo-600 hover:underline">Forgot password?</a>
                </div>
            </form>
        </div>)
}

export default Auth