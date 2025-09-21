import React, { useState } from "react";
import axios, { type AxiosResponse } from "axios";
import { User, Lock, Mail, Eye, EyeOff, Save, X } from "lucide-react";
import { useAuth } from "../../context/AuthContext"; // adjust path
import "./Profile.css";

interface ChangePasswordDto {
  currentPassword: string;
  newPassword: string;
  confirmPassword: string;
}

interface ChangeEmailDto {
  newEmail: string;
  password: string;
}

const API_URL = "http://localhost:5286/api";

const ProfilePage: React.FC = () => {
  const { user, setUser } = useAuth();
  const [activeTab, setActiveTab] = useState<"password" | "email">("password");

  const [showCurrentPassword, setShowCurrentPassword] = useState(false);
  const [showNewPassword, setShowNewPassword] = useState(false);
  const [showConfirmPassword, setShowConfirmPassword] = useState(false);
  const [showEmailPassword, setShowEmailPassword] = useState(false);

  const [passwordForm, setPasswordForm] = useState<ChangePasswordDto>({
    currentPassword: "",
    newPassword: "",
    confirmPassword: "",
  });

  const [emailForm, setEmailForm] = useState<ChangeEmailDto>({
    newEmail: "",
    password: "",
  });

  const [loading, setLoading] = useState(false);
  const [message, setMessage] = useState<{
    type: "success" | "error";
    text: string;
  } | null>(null);

  const clearMessage = () => setMessage(null);

  const getAuthHeader = () => ({
    Authorization: user ? `Bearer ${user.token}` : "",
    "Content-Type": "application/json",
  });

  const handlePasswordChange = async (e: React.FormEvent) => {
    e.preventDefault();
    if (passwordForm.newPassword !== passwordForm.confirmPassword) {
      setMessage({ type: "error", text: "New passwords do not match" });
      return;
    }
    if (passwordForm.newPassword.length < 6) {
      setMessage({
        type: "error",
        text: "Password must be at least 6 characters",
      });
      return;
    }
    setLoading(true);
    setMessage(null);

    try {
      const response: AxiosResponse<string> = await axios.post(
        `${API_URL}/account/change-password`,
        passwordForm,
        { headers: getAuthHeader() }
      );
      setMessage({ type: "success", text: response.data });
      setPasswordForm({
        currentPassword: "",
        newPassword: "",
        confirmPassword: "",
      });
    } catch (error: any) {
      setMessage({
        type: "error",
        text: error.response?.data || "Error changing password",
      });
    } finally {
      setLoading(false);
    }
  };

  const handleEmailChange = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!/\S+@\S+\.\S+/.test(emailForm.newEmail)) {
      setMessage({ type: "error", text: "Enter a valid email address" });
      return;
    }
    setLoading(true);
    setMessage(null);

    try {
      const response: AxiosResponse<string> = await axios.post(
        `${API_URL}/account/change-email`,
        emailForm,
        { headers: getAuthHeader() }
      );
      setMessage({ type: "success", text: response.data });
      if (user) setUser({ ...user, email: emailForm.newEmail });
      setEmailForm({ newEmail: "", password: "" });
    } catch (error: any) {
      setMessage({
        type: "error",
        text: error.response?.data || "Error updating email",
      });
    } finally {
      setLoading(false);
    }
  };

  if (!user) {
    return (
      <div className="profile-container">
        <div className="loading">Please log in to access your profile.</div>
      </div>
    );
  }

  return (
    <div className="profile-container">
      <div className="page-header">
        <h1>Account Profile</h1>
        <p>Manage your account settings and preferences</p>
      </div>

      <div className="profile-content">
        <div className="user-info-section">
          <div className="user-avatar">
            <User size={48} />
          </div>
          <div className="user-details">
            <h2>{user.userName}</h2>
            <p className="user-email">{user.email}</p>
          </div>
        </div>

        {message && (
          <div className={`message ${message.type}`}>
            <span>{message.text}</span>
            <button onClick={clearMessage} className="message-close">
              <X size={16} />
            </button>
          </div>
        )}

        <div className="settings-section">
          <div className="tab-navigation">
            <button
              className={`tab-btn ${activeTab === "password" ? "active" : ""}`}
              onClick={() => setActiveTab("password")}
            >
              <Lock size={18} /> Change Password
            </button>
            <button
              className={`tab-btn ${activeTab === "email" ? "active" : ""}`}
              onClick={() => setActiveTab("email")}
            >
              <Mail size={18} /> Change Email
            </button>
          </div>

          <div className="settings-form-container">
            {activeTab === "password" && (
              <div className="settings-form">
                <h3>Change Password</h3>
                {/* Password fields with show/hide logic */}
                {["currentPassword", "newPassword", "confirmPassword"].map(
                  (field) => (
                    <div className="form-group" key={field}>
                      <label>
                        {field === "currentPassword"
                          ? "Current Password"
                          : field === "newPassword"
                            ? "New Password"
                            : "Confirm New Password"}
                      </label>
                      <div className="password-input-group">
                        <input
                          type={
                            (
                              field === "currentPassword"
                                ? showCurrentPassword
                                : field === "newPassword"
                                  ? showNewPassword
                                  : showConfirmPassword
                            )
                              ? "text"
                              : "password"
                          }
                          value={passwordForm[field as keyof ChangePasswordDto]}
                          onChange={(e) =>
                            setPasswordForm((prev) => ({
                              ...prev,
                              [field]: e.target.value,
                            }))
                          }
                          placeholder={`Enter ${field.replace(/([A-Z])/g, " $1").toLowerCase()}`}
                        />
                        <button
                          type="button"
                          onClick={() =>
                            field === "currentPassword"
                              ? setShowCurrentPassword(!showCurrentPassword)
                              : field === "newPassword"
                                ? setShowNewPassword(!showNewPassword)
                                : setShowConfirmPassword(!showConfirmPassword)
                          }
                        >
                          {(
                            field === "currentPassword"
                              ? showCurrentPassword
                              : field === "newPassword"
                                ? showNewPassword
                                : showConfirmPassword
                          ) ? (
                            <EyeOff size={18} />
                          ) : (
                            <Eye size={18} />
                          )}
                        </button>
                      </div>
                    </div>
                  )
                )}

                <button onClick={handlePasswordChange} disabled={loading}>
                  <Save size={18} />{" "}
                  {loading ? "Changing..." : "Change Password"}
                </button>
              </div>
            )}

            {activeTab === "email" && (
              <div className="settings-form">
                <h3>Change Email</h3>
                <div className="form-group">
                  <label>Current Email</label>
                  <input
                    type="email"
                    value={user.email}
                    disabled
                    className="disabled-input"
                  />
                </div>

                <div className="form-group">
                  <label>New Email</label>
                  <input
                    type="email"
                    value={emailForm.newEmail}
                    onChange={(e) =>
                      setEmailForm((prev) => ({
                        ...prev,
                        newEmail: e.target.value,
                      }))
                    }
                    placeholder="Enter new email"
                  />
                </div>

                <div className="form-group">
                  <label>Current Password</label>
                  <div className="password-input-group">
                    <input
                      type={showEmailPassword ? "text" : "password"}
                      value={emailForm.password}
                      onChange={(e) =>
                        setEmailForm((prev) => ({
                          ...prev,
                          password: e.target.value,
                        }))
                      }
                      placeholder="Enter current password"
                    />
                    <button
                      type="button"
                      onClick={() => setShowEmailPassword(!showEmailPassword)}
                    >
                      {showEmailPassword ? (
                        <EyeOff size={18} />
                      ) : (
                        <Eye size={18} />
                      )}
                    </button>
                  </div>
                </div>

                <button onClick={handleEmailChange} disabled={loading}>
                  <Save size={18} /> {loading ? "Updating..." : "Update Email"}
                </button>
              </div>
            )}
          </div>
        </div>
      </div>
    </div>
  );
};

export default ProfilePage;
