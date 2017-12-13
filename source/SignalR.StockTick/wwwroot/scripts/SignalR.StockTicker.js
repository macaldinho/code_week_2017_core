//deploy
//let connection = new signalR.HubConnection("http://signalr-stockticker.azurewebsites.net/stocks");

//Local
let connection = new signalR.HubConnection("/stocks");

connection.start().then(function () {

    connection.invoke("GetAllStocks").then(function (stocks) {
        console.log("get ");
        console.log(stocks);
    });

    connectBraodcast();
});

function connectBraodcast() {
    connection.on("broadStocks",
        function (stocks) {
            console.log("broad ");
            console.log(stocks);
        });

    connection.on("broadStock",
        function (stock) {
            console.log("broadsingle ");
            console.log(stock);
        });
}