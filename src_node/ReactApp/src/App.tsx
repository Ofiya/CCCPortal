import { createBrowserRouter } from "react-router";
import { RouterProvider } from "react-router/dom";
import  Auth from "./features/Auth";
import Index from "./features/Screens/Index";
import { homeRoutes } from "./routes/homeRoutes";


function App() {


  const router = createBrowserRouter([
    {
      path: "/auth",
      element: <Auth />,
    },
    {
      path: "/",
      element: <Index />,
      children: [...homeRoutes]
    },
  ]);


  return (
    <>
      <RouterProvider router={router} />,
    </>
  )
}

export default App
