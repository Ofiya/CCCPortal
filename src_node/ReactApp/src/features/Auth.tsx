import React, { useState } from "react";
import { useNavigate } from "react-router";
// import { useAxios } from "../utils/hooks/useAxios";
import axios from "axios";

const Auth: React.FC = () => {
    const navigate = useNavigate();
    const [formData, setFormData] = useState({
        email: '',
        password: '',
    });

    const handleChange = (e: React.ChangeEvent<HTMLInputElement>) => {
        setFormData({
            ...formData,
            [e.target.name]: e.target.value
        });
    };

    const handleSubmit = (e: React.FormEvent<HTMLFormElement>) => {
        e.preventDefault();
        
        axios.post("https://redemptionfe.azurewebsites.net/api/Auth/login", {
            email: formData.email,
            password: formData.password,
        }).then((response) => {
            console.log(response.data);
            navigate("/dashboard");
        }).catch((error) => {
            console.error("There was an error!", error);
        });

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
                    <input name="email" onChange={handleChange} type="email" className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-indigo-500" placeholder="Enter your email" required />
                </div>

                <div className="mb-6">
                    <label className="block text-gray-700 text-sm font-semibold mb-2" htmlFor="password">Password</label>
                    <input name="password" onChange={handleChange} type="password" className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-indigo-500" placeholder="Enter your password" required />
                </div>

                <button type="submit" className="w-full bg-indigo-600 text-white font-semibold py-2 px-4 rounded-md hover:bg-indigo-700 focus:outline-none focus:ring-2 focus:ring-indigo-500 focus:ring-offset-2">
                    Login
                </button>

                <div className="mt-4 text-center text-sm text-gray-600">
                    <a href="#" onClick={(e) => e.preventDefault()} className="text-indigo-600 hover:underline">Forgot password?</a>
                </div>
            </form>
        </div>)
}

export default Auth