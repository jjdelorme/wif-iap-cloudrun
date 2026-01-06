document.getElementById('fetchBtn').addEventListener('click', async () => {
    const resultDiv = document.getElementById('result');
    resultDiv.textContent = 'Loading...';

    try {
        const response = await fetch('/api/data');

        if (!response.ok) {
            throw new Error(`HTTP error! status: ${response.status}`);
        }

        const data = await response.json();
        resultDiv.textContent = JSON.stringify(data, null, 2);

    } catch (error) {
        console.error('Fetch error:', error);
        resultDiv.textContent = 'Error fetching data: ' + error.message;
    }
});
