//export const BASE_URL = "https://meet-app-udl.azurewebsites.net";
//export const BASE_CHAT_HUB_URL= "https://meet-app-udl.azurewebsites.net/hubs/chat-hub";

export const BASE_URL = import.meta.env.VITE_API_BASE_URL || "http://localhost:5000";
export const BASE_CHAT_HUB_URL = `${BASE_URL}/hubs/chat-hub`;

