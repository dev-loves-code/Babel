import {
  createContext,
  useContext,
  useEffect,
  useState,
  type ReactNode,
} from "react";
import * as signalR from "@microsoft/signalr";
import { useAuth } from "../../context/AuthContext";
import "./Notification.css";

interface NotificationContextType {
  notify: (message: string) => void;
}

const NotificationContext = createContext<NotificationContextType | undefined>(
  undefined
);

export const NotificationProvider = ({ children }: { children: ReactNode }) => {
  const { user } = useAuth();
  const [notifications, setNotifications] = useState<string[]>([]);

  useEffect(() => {
    if (!user?.token) return;

    const connection = new signalR.HubConnectionBuilder()
      .withUrl("http://localhost:5286/notificationHub", {
        accessTokenFactory: () => user.token,
      })
      .withAutomaticReconnect()
      .configureLogging(signalR.LogLevel.Error) // silence info/warning spam
      .build();

    connection
      .start()
      .then(() => {
        console.log("✅ SignalR connected");
      })
      .catch((err) => {
        // Ignore harmless negotiation aborts
        if (
          err instanceof Error &&
          err.message.includes("stopped during negotiation")
        ) {
          console.debug("SignalR negotiation upgrade (harmless)");
        } else {
          console.error("❌ SignalR failed to connect:", err);
        }
      });

    connection.on("ReceiveMessage", (message: string) => {
      addNotification(message);
    });

    return () => {
      connection.stop();
    };
  }, [user?.token]);

  const addNotification = (message: string) => {
    setNotifications((prev) => [...prev, message]);
    setTimeout(() => {
      setNotifications((prev) => prev.slice(1));
    }, 5000);
  };

  const notify = (message: string) => {
    addNotification(message);
  };

  const dismissNotification = (index: number) => {
    setNotifications((prev) => prev.filter((_, i) => i !== index));
  };

  return (
    <NotificationContext.Provider value={{ notify }}>
      {children}

      {/* Global popup container */}
      <div className="notification-container">
        {notifications.map((n, i) => (
          <div key={i} className="notification-popup">
            <span>{n}</span>
            <button
              className="close-btn"
              onClick={() => dismissNotification(i)}
            >
              ✕
            </button>
          </div>
        ))}
      </div>
    </NotificationContext.Provider>
  );
};

export const useNotification = () => {
  const ctx = useContext(NotificationContext);
  if (!ctx)
    throw new Error("useNotification must be used within NotificationProvider");
  return ctx;
};
