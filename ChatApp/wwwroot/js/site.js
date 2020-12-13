// Please see documentation at https://docs.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.
let sessionId = "";
$.ajax(
    {
        url: "https://localhost:5001/api/chat/session", //TODO: get base URL from config file
        type: "GET",
        success: function (result) {
            sessionId = result.sessionId;                         //TODO: use JSON response
            addTextToChatWindow("Created session: " + JSON.stringify(result));
            if (result.statusAsString == "Working" || result.statusAsString == "Waiting")
                window.setTimeout(doPolling, 1000);
        },
        error: function (error) {
            console.log(result);
        }
    }
);

function addTextToChatWindow(text) {
    document.getElementById("chat-window").value += text + "\r\n";
}

// The simplest continuous polling
function doPolling() {
    $.ajax(
        {
            url: "https://localhost:5001/api/chat/poll?sessionId=" + sessionId, //TODO: get base URL from config file
            type: "GET",
            success: function (result) {
                addTextToChatWindow("Session pooling: " + JSON.stringify(result));
                if (result.statusAsString == "Working" || result.statusAsString == "Waiting") {
                    window.setTimeout(doPolling, 1000);
                    return;
                }
                addTextToChatWindow("Stop pooling." );
            },
            error: function (error) {
                addTextToChatWindow(JSON.stringify(error));
                addTextToChatWindow("Stop pooling.");
            }
        }
    );  
}

//TODO: autoscroll down is nice to have



