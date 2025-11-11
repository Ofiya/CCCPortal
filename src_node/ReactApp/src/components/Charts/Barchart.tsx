import {
    Chart as ChartJS,
    CategoryScale,
    LinearScale,
    BarElement,
    Title,
    Tooltip,
    Legend,
} from "chart.js";
import { Bar } from "react-chartjs-2";

// Register the necessary Chart.js components (required for Chart.js v3+)
ChartJS.register(
    CategoryScale,
    LinearScale,
    BarElement,
    Title,
    Tooltip,
    Legend
);

// Default export: a self-contained React component you can drop into any app
export default function BarChart({
    title = "" , //Chart title
    labels = [
        "January",
        "February",
        "March",
        "April",
        "May",
        "June",
        "July",
    ],
}) {
    // Sample dataset — replace with your own data or pass via props
    const data = {
        labels,
        datasets: [
            {
                label: "",
                data: [12, 19, 7, 14, 20, 9, 15],
                // For Chart.js v5 you can specify backgroundColor as an array or single value
                backgroundColor: [
                    "rgba(75, 192, 192, 0.6)",
                    "rgba(54, 162, 235, 0.6)",
                    "rgba(255, 206, 86, 0.6)",
                    "rgba(255, 99, 132, 0.6)",
                    "rgba(153, 102, 255, 0.6)",
                    "rgba(201, 203, 207, 0.6)",
                    "rgba(100, 181, 246, 0.6)",
                ],
                borderColor: "rgba(0,0,0,0.1)",
                borderWidth: 1,
            },
        ],
    };

    const options = {
        responsive: true,
        maintainAspectRatio: false,
        plugins: {
            legend: {
                position: "top",
            },
            title: {
                display: !!title,
                text: title,
                font: {
                    size: 16,
                    weight: "600",
                },
            },
            tooltip: {
                enabled: true,
                mode: "index",
                intersect: false,
            },
        },
        scales: {
            x: {
                grid: {
                    display: false,
                },
                title: {
                    display: true,
                    text: "Month",
                },
            },
            y: {
                beginAtZero: true,
                title: {
                    display: true,
                    text: "",
                },
            },
        },
    };

    return (
        // @ts-ignore
        <Bar data={data} options={options} />
    );
}

/*
Usage:
1. Install dependencies:
   npm install chart.js react-chartjs-2
   (Chart.js v5 is supported — ensure you're using a compatible react-chartjs-2 version.)

2. Import and use in your React app:
   import ChartJS5BarChart from './ChartJS5_BarChart';

   function App() {
     return (
       <div className="p-8">
         <ChartJS5BarChart />
       </div>
     )
   }

3. To provide your own data, pass `labels` prop and modify the dataset inside the component or wrap the data into a prop.
*/
