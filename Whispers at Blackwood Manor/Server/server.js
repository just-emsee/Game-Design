const express = require("express");
const http = require("http");
const WebSocket = require("ws");
const path = require("path");

const app = express();
const server = http.createServer(app);
const wss = new WebSocket.Server({ server });

const PORT = 3000;
const SERVER_SESSION_ID = Date.now().toString();

app.use(express.static(path.join(__dirname, "public")));

let players = [];
let socketsByClientId = {};
let lastPayloadByClientId = {};

wss.on("connection", (socket) => {
    console.log("A device connected.");

    socket.clientId = null;
    
        socket.send(JSON.stringify({
        type: "serverSession",
        sessionId: SERVER_SESSION_ID
    }));

    socket.on("message", (message) => {
        try {
            const data = JSON.parse(message);

            if (data.type === "join") {
                handleJoin(socket, data);
            }

            if (data.type === "reconnect") {
                handleReconnect(socket, data);
            }
            if (data.type === "sendToClient") {
                handleSendToClient(data);
            }
            if (data.type === "submitMurder") {
                handleSubmitMurder(data);
            }
            
            if (data.type === "submitVictimWhisper") {
                broadcast({
                    type: "submitVictimWhisper",
                    whisperText: data.whisperText
                });
            }

            if (data.type === "requestNextVictimWhisper") {
                broadcast({
                    type: "requestNextVictimWhisper"
                });
            }


        } catch (error) {
            console.log("Invalid message:", message.toString());
        }
    });

    socket.on("close", () => {
        console.log("A device disconnected.");

        if (socket.clientId) {
            const player = players.find(p => p.clientId === socket.clientId);

            if (player) {
                player.online = false;
                broadcastPlayerList();
            }

            delete socketsByClientId[socket.clientId];
        }
    });
});

function handleJoin(socket, data) {
    const clientId = data.clientId;
    const name = data.name.trim();

    if (!clientId || !name) {
        return;
    }

    socket.clientId = clientId;
    socketsByClientId[clientId] = socket;

    let existingPlayer = players.find(p => p.clientId === clientId);

    if (existingPlayer) {
        existingPlayer.name = name;
        existingPlayer.online = true;

        console.log(`${existingPlayer.name} rejoined.`);
    } else {
        const nameTaken = players.some(p => 
            p.name.toLowerCase() === name.toLowerCase() &&
            p.clientId !== clientId
        );

        if (nameTaken) {
            socket.send(JSON.stringify({
                type: "joinError",
                message: "That name is already taken."
            }));
            return;
        }

        const player = {
            clientId: clientId,
            name: name,
            online: true
        };

        players.push(player);

        console.log(`${player.name} joined!`);
    }

    resendLastPayloadIfAny(socket, clientId);

    broadcastPlayerList();
}

function handleReconnect(socket, data) {
    const clientId = data.clientId;

    if (!clientId) {
        return;
    }

    socket.clientId = clientId;
    socketsByClientId[clientId] = socket;

    const player = players.find(p => p.clientId === clientId);

    if (player) {
        player.online = true;

        console.log(`${player.name} reconnected.`);

        socket.send(JSON.stringify({
            type: "reconnected",
            name: player.name
        }));

        resendLastPayloadIfAny(socket, clientId);

        broadcastPlayerList();
    }
}

function broadcastPlayerList() {
    broadcast({
        type: "playerList",
        players: players
    });

    console.log("Current players:", players.map(p => `${p.name}${p.online ? "" : " (offline)"}`));
}

function broadcast(data) {
    const message = JSON.stringify(data);

    wss.clients.forEach((client) => {
        if (client.readyState === WebSocket.OPEN) {
            client.send(message);
        }
    });
}

function handleSendToClient(data) {
    const targetClientId = data.targetClientId;
    const payload = data.payload;

    if (!targetClientId || !payload) {
        return;
    }

    // Remember the last screen/payload sent to this player.
    // This lets them reload or reconnect and return to their clue screen.
    lastPayloadByClientId[targetClientId] = payload;

    const targetSocket = socketsByClientId[targetClientId];

    if (!targetSocket || targetSocket.readyState !== WebSocket.OPEN) {
        console.log("Saved private message, but target is not connected:", targetClientId, payload.type);
        return;
    }

    targetSocket.send(JSON.stringify(payload));

    console.log("Sent private message to:", targetClientId, payload.type);
}

function resendLastPayloadIfAny(socket, clientId) {
    const lastPayload = lastPayloadByClientId[clientId];

    if (!lastPayload) {
        return;
    }

    if (socket.readyState === WebSocket.OPEN) {
        socket.send(JSON.stringify(lastPayload));
        console.log("Resent last screen to:", clientId, lastPayload.type);
    }
}

function handleSubmitMurder(data) {
    console.log("Murder submitted:");
    console.log("Victim:", data.victimName);
    console.log("Weapon:", data.weapon);
    console.log("Location:", data.location);

    broadcast({
        type: "murderSubmitted",
        killerClientId: data.killerClientId,
        victimClientId: data.victimClientId,
        victimName: data.victimName,
        weapon: data.weapon,
        location: data.location
    });
}

server.listen(PORT, "0.0.0.0", () => {
    console.log(`Server running on http://localhost:${PORT}`);
    console.log(`Open this on your phone using your laptop IP address.`);
});