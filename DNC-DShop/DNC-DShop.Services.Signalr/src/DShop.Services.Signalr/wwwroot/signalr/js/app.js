'use strict';
(function() {
    const $jwt = document.getElementById("jwt");
    const $connect = document.getElementById("connect");
    const $messages = document.getElementById("messages");
    const connection = new signalR.HubConnectionBuilder()
        .withUrl('http://localhost:5007/dshop')
        .configureLogging(signalR.LogLevel.Information)
        .build();
    // const jwt = 'eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiJkNDRkOGQ0YWJmNmE0MWVkOTVhMmNlNTMxZWQ2MmM4MCIsInVuaXF1ZV9uYW1lIjoiZDQ0ZDhkNGFiZjZhNDFlZDk1YTJjZTUzMWVkNjJjODAiLCJqdGkiOiI5NTExODRiYy03OWU2LTQwMDAtOTQ0OC1jZTYyMzNlZGEyMGQiLCJpYXQiOiIxNTY4OTg5MTQ2NjQwIiwiaHR0cDovL3NjaGVtYXMubWljcm9zb2Z0LmNvbS93cy8yMDA4LzA2L2lkZW50aXR5L2NsYWltcy9yb2xlIjoidXNlciIsIm5iZiI6MTU2ODk4OTE0NiwiZXhwIjoxNTY4OTkwOTQ2LCJpc3MiOiJkc2hvcC1pZGVudGl0eS1zZXJ2aWNlIn0.n4Eb6dPaYfplfH5JTANyn9SIOeJVIWdBgrCD8FiyQYE';
    // appendMessage("Connecting to DShop Hub...");
    // connection.start()
    //     .then(() => {
    //     connection.invoke('initializeAsync', jwt);
    // })
    // .catch(err => appendMessage(err));

    $connect.onclick = function() {
        const jwt = $jwt.value;
        if (!jwt || /\s/g.test(jwt)){
            alert('Invalid JWT.')
            return;
        }

        appendMessage("Connecting to DShop Hub...");
        connection.start()
            .then(() => {
            connection.invoke('initializeAsync', $jwt.value);
        })
        .catch(err => appendMessage(err));
    }
    
    connection.on('connected', _ => {
        appendMessage("Connected.", "primary");
    });

    connection.on('disconnected', _ => {
        appendMessage("Disconnected, invalid token.", "danger");
    });

    connection.on('operation_pending', (operation) => {
        appendMessage('Operation pending.', "light", operation);
    });

    connection.on('operation_completed', (operation) => {
        appendMessage('Operation completed.', "success", operation);
    });

    connection.on('operation_rejected', (operation) => {
        appendMessage('Operation rejected.', "danger", operation);
    });
    
    function appendMessage(message, type, data) {
        var dataInfo = "";
        if (data) {
            dataInfo += "<div>" + JSON.stringify(data) + "</div>";
        }
        $messages.innerHTML += `<li class="list-group-item list-group-item-${type}">${message} ${dataInfo}</li>`;
    }
})();