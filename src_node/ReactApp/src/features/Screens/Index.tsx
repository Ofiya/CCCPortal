import { Outlet } from 'react-router'
import Layout from '../../components/Layout/Layout'

const Index = () => {
  return (
    <Layout>
        <Outlet />
    </Layout>
  )
}

export default Index