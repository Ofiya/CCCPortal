const Settings = () => {
    return (
       <div id="settings-page" className="page p-6 admin-only-content">
          <div className="mb-6">
            <h2 className="text-2xl font-bold text-gray-800">Settings</h2>
            <p className="text-gray-600">Manage system configuration and notifications</p>
          </div>

          <div className="grid grid-cols-1 lg:grid-cols-3 gap-6">
            {/* <!-- General Settings --> */}
            <div className="bg-white rounded-lg shadow p-6 lg:col-span-2">
              <h3 className="text-lg font-semibold text-gray-800 mb-4">General Settings</h3>
              <div className="space-y-4">
                <div>
                  <label className="block text-sm font-medium text-gray-700 mb-1">Church Name</label>
                  <input type="text" className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-indigo-500" value="CCC Redemption Parish" />
                </div>
                <div>
                  <label className="block text-sm font-medium text-gray-700 mb-1">Address</label>
                  <input type="text" className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-indigo-500" value="123 Church Street, City, State, ZIP" />
                </div>
                <div>
                  <label className="block text-sm font-medium text-gray-700 mb-1">Phone</label>
                  <input type="text" className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-indigo-500" value="555-123-4567" />
                </div>
                <div>
                  <label className="block text-sm font-medium text-gray-700 mb-1">Email</label>
                  <input type="email" className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-indigo-500" value="info@cccredemptionparish.org" />
                </div>
                <div>
                  <label className="block text-sm font-medium text-gray-700 mb-1">Sunday Service Time</label>
                  <input type="text" className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-indigo-500" value="10:00 AM - 12:00 PM" />
                </div>
                <div className="flex justify-end">
                  <button className="bg-indigo-600 hover:bg-indigo-700 text-white font-medium py-2 px-4 rounded-md">
                    Save Settings
                  </button>
                </div>
              </div>
            </div>

            {/* <!-- User Management --> */}
            <div className="bg-white rounded-lg shadow p-6">
              <h3 className="text-lg font-semibold text-gray-800 mb-4">User Management</h3>
              <p className="text-sm text-gray-600 mb-4">Manage access levels and user permissions</p>
              <button id="add-user-btn" className="w-full bg-indigo-600 hover:bg-indigo-700 text-white font-medium py-2 px-4 rounded-md mb-4">
                Add New User
              </button>
              <div className="space-y-3" id="users-list">
                {/* <!-- Will be populated dynamically --> */}
              </div>
            </div>

            {/* <!-- Notification Settings --> */}
            <div className="bg-white rounded-lg shadow p-6 lg:col-span-2">
              <h3 className="text-lg font-semibold text-gray-800 mb-4">Notification Settings</h3>
              <p className="text-sm text-gray-500 mb-4">Configure email and SMS notifications for birthday wishes and follow-ups</p>
              
              <div className="mb-6">
                <h4 className="font-medium text-gray-700 mb-2">Email Provider</h4>
                <div className="space-y-3">
                  <div>
                    <label className="block text-sm font-medium text-gray-700 mb-1">Provider</label>
                    <select className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-indigo-500">
                      <option selected>SendGrid</option>
                      <option>Mailgun</option>
                      <option>SMTP</option>
                    </select>
                  </div>
                  <div>
                    <label className="block text-sm font-medium text-gray-700 mb-1">API Key</label>
                    <input type="password" placeholder="Enter API key" className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-indigo-500" />
                    <p className="text-xs text-gray-500 mt-1">This will be stored in .env as EMAIL_API_KEY</p>
                  </div>
                  <div className="flex items-center mt-2">
                    <input type="checkbox" id="enable-birthday-emails" className="h-4 w-4 text-indigo-600 focus:ring-indigo-500 border-gray-300 rounded" />
                    <label htmlFor="enable-birthday-emails" className="ml-2 block text-sm text-gray-700">Enable birthday email notifications</label>
                  </div>
                </div>
              </div>
              
              <div className="mb-6">
                <h4 className="font-medium text-gray-700 mb-2">SMS Provider</h4>
                <div className="space-y-3">
                  <div>
                    <label className="block text-sm font-medium text-gray-700 mb-1">Provider</label>
                    <select className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-indigo-500">
                      <option selected>Twilio</option>
                      <option>Vonage (Nexmo)</option>
                      <option>AWS SNS</option>
                    </select>
                  </div>
                  <div>
                    <label className="block text-sm font-medium text-gray-700 mb-1">Account SID</label>
                    <input type="text" placeholder="Enter Account SID" className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-indigo-500" />
                    <p className="text-xs text-gray-500 mt-1">This will be stored in .env as SMS_ACCOUNT_SID</p>
                  </div>
                  <div>
                    <label className="block text-sm font-medium text-gray-700 mb-1">Auth Token</label>
                    <input type="password" placeholder="Enter Auth Token" className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-indigo-500" />
                    <p className="text-xs text-gray-500 mt-1">This will be stored in .env as SMS_AUTH_TOKEN</p>
                  </div>
                  <div>
                    <label className="block text-sm font-medium text-gray-700 mb-1">From Number</label>
                    <input type="text" placeholder="+1234567890" className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-indigo-500" />
                    <p className="text-xs text-gray-500 mt-1">This will be stored in .env as SMS_FROM_NUMBER</p>
                  </div>
                  <div className="flex items-center mt-2">
                    <input type="checkbox" id="enable-birthday-sms" className="h-4 w-4 text-indigo-600 focus:ring-indigo-500 border-gray-300 rounded" />
                    <label htmlFor="enable-birthday-sms" className="ml-2 block text-sm text-gray-700">Enable birthday SMS notifications</label>
                  </div>
                </div>
              </div>
              
              <div className="flex justify-end">
                <button className="bg-indigo-600 hover:bg-indigo-700 text-white font-medium py-2 px-4 rounded-md">
                  Save Notification Settings
                </button>
              </div>
            </div>

            {/* <!-- Database Configuration --> */}
            <div className="bg-white rounded-lg shadow p-6">
              <h3 className="text-lg font-semibold text-gray-800 mb-4">Database Configuration</h3>
              <div className="space-y-3">
                <div>
                  <label className="block text-sm font-medium text-gray-700 mb-1">Azure SQL Connection String</label>
                  <input type="password" value="Server=tcp:redemptionfe.database.windows.net,1433;Database=RedemptionDB;User ID=your-username;Password=your-password;Encrypt=true;TrustServerCertificate=false;Connection Timeout=30;" className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-indigo-500" />
                  <p className="text-xs text-gray-500 mt-1">Stored in .env as DB_CONNECTION_STRING</p>
                </div>
                <div>
                  <label className="block text-sm font-medium text-gray-700 mb-1">Database Name</label>
                  <input type="text" value="RedemptionDB" className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-indigo-500" />
                </div>
              </div>
            </div>
          </div>
        </div>
    )
}

export default Settings