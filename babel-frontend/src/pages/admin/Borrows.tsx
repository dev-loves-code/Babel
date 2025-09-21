import { useState, useEffect } from "react";
import { useAuth } from "../../context/AuthContext";
import "./AdminPannel.css";

interface GetBorrowDto {
  id: number;
  bookId: number;
  bookTitle: string;
  userId: string;
  userName: string;
  startDate: string;
  dueDate: string;
  returnDate?: string;
  status: number;
  hasPendingReturnRequest: boolean;
}

interface GetReturnRequestDto {
  id: number;
  borrowId: number;
  bookTitle: string;
  userId: string;
  userName: string;
  requestDate: string;
  status: number;
  startDate: string;
  dueDate: string;
  isOverdue: boolean;
}

const BorrowStatus = {
  Active: "Active",
  Returned: "Returned",
  ReturnedPastDue: "ReturnedPastDue",
  PastDue: "PastDue",
} as const;

const API_URL = "http://localhost:5286/api";

export default function BorrowsPage() {
  const { user } = useAuth();
  const [activeTab, setActiveTab] = useState<"borrows" | "requests">("borrows");

  // Borrows state
  const [borrows, setBorrows] = useState<GetBorrowDto[]>([]);
  const [borrowsLoading, setBorrowsLoading] = useState(false);
  const [borrowFilters, setBorrowFilters] = useState({
    userId: "",
    userName: "",
    bookTitle: "",
    status: "",
    includeReturned: false,
    startDateFrom: "",
    startDateTo: "",
  });

  // Return requests state
  const [returnRequests, setReturnRequests] = useState<GetReturnRequestDto[]>(
    []
  );
  const [requestsLoading, setRequestsLoading] = useState(false);
  const [requestFilters, setRequestFilters] = useState({
    userId: "",
    userName: "",
    status: "",
    requestDateFrom: "",
    requestDateTo: "",
  });

  // Processing state
  const [processingRequestId, setProcessingRequestId] = useState<number | null>(
    null
  );
  const [processingDirectReturn, setProcessingDirectReturn] = useState<
    number | null
  >(null);

  const getAuthHeader = () => ({
    Authorization: user ? `Bearer ${user.token}` : "",
    "Content-Type": "application/json",
  });

  // Fetch borrows
  const fetchBorrows = async () => {
    setBorrowsLoading(true);
    try {
      const params = new URLSearchParams();
      if (borrowFilters.userId) params.append("userId", borrowFilters.userId);
      if (borrowFilters.userName)
        params.append("userName", borrowFilters.userName);
      if (borrowFilters.bookTitle)
        params.append("bookTitle", borrowFilters.bookTitle);
      if (borrowFilters.status) params.append("status", borrowFilters.status);
      if (borrowFilters.includeReturned)
        params.append("includeReturned", "true");
      if (borrowFilters.startDateFrom)
        params.append("startDateFrom", borrowFilters.startDateFrom);
      if (borrowFilters.startDateTo)
        params.append("startDateTo", borrowFilters.startDateTo);

      const response = await fetch(`${API_URL}/borrow?${params.toString()}`, {
        headers: getAuthHeader(),
      });
      if (response.ok) {
        const data: GetBorrowDto[] = await response.json();
        setBorrows(data);
        console.log(data);
      } else {
        console.error("Error fetching borrows:", response.statusText);
      }
    } catch (err) {
      console.error("Error fetching borrows:", err);
    } finally {
      setBorrowsLoading(false);
    }
  };

  // Fetch return requests
  const fetchReturnRequests = async () => {
    setRequestsLoading(true);
    try {
      const params = new URLSearchParams();
      if (requestFilters.userId) params.append("userId", requestFilters.userId);
      if (requestFilters.userName)
        params.append("userName", requestFilters.userName);
      if (requestFilters.status) params.append("status", requestFilters.status);
      if (requestFilters.requestDateFrom)
        params.append("requestDateFrom", requestFilters.requestDateFrom);
      if (requestFilters.requestDateTo)
        params.append("requestDateTo", requestFilters.requestDateTo);

      const response = await fetch(
        `${API_URL}/returnrequest?${params.toString()}`,
        {
          headers: getAuthHeader(),
        }
      );
      if (response.ok) {
        const data: GetReturnRequestDto[] = await response.json();
        setReturnRequests(data);
      } else {
        console.error("Error fetching return requests:", response.statusText);
      }
    } catch (err) {
      console.error("Error fetching return requests:", err);
    } finally {
      setRequestsLoading(false);
    }
  };

  // Process return request
  const processReturnRequest = async (requestId: number, status: number) => {
    setProcessingRequestId(requestId);
    try {
      const response = await fetch(
        `${API_URL}/returnrequest/${requestId}/process`,
        {
          method: "PUT",
          headers: getAuthHeader(),
          body: JSON.stringify({ status }),
        }
      );
      if (response.ok) {
        fetchReturnRequests();
        fetchBorrows();
        alert("Return request processed successfully!");
      } else {
        console.error("Error processing return request:", response.statusText);
        alert("Error processing return request");
      }
    } catch (err) {
      console.error("Error processing return request:", err);
      alert("Error processing return request");
    } finally {
      setProcessingRequestId(null);
    }
  };

  // Direct return
  const handleDirectReturn = async (borrowId: number) => {
    setProcessingDirectReturn(borrowId);
    try {
      const response = await fetch(`${API_URL}/returnrequest/direct-return`, {
        method: "POST",
        headers: getAuthHeader(),
        body: JSON.stringify({ borrowId }),
      });
      if (response.ok) {
        fetchBorrows();
        alert("Direct return processed successfully!");
      } else {
        console.error("Error processing direct return:", response.statusText);
        alert("Error processing direct return");
      }
    } catch (err) {
      console.error("Error processing direct return:", err);
      alert("Error processing direct return");
    } finally {
      setProcessingDirectReturn(null);
    }
  };

  useEffect(() => {
    if (activeTab === "borrows") {
      fetchBorrows();
    } else {
      fetchReturnRequests();
    }
  }, [activeTab]);

  const formatDate = (dateString: string) => {
    return new Date(dateString).toLocaleDateString();
  };

  const canDirectReturn = (borrow: GetBorrowDto) => {
    return borrow.status === 0 || borrow.status === 3;
  };

  return (
    <div className="borrows-page">
      <h1 className="page-title">Admin - Borrows Management</h1>

      {/* Tab Navigation */}
      <div className="tab-navigation">
        <button
          onClick={() => setActiveTab("borrows")}
          className={`tab-button ${activeTab === "borrows" ? "active" : ""}`}
        >
          All Borrows
        </button>
        <button
          onClick={() => setActiveTab("requests")}
          className={`tab-button ${activeTab === "requests" ? "active" : ""}`}
        >
          Return Requests
        </button>
      </div>

      {activeTab === "borrows" && (
        <div>
          {/* Borrow Filters */}
          <div className="filters-container">
            <input
              type="text"
              placeholder="User ID"
              value={borrowFilters.userId}
              onChange={(e) =>
                setBorrowFilters({ ...borrowFilters, userId: e.target.value })
              }
              className="filter-input"
            />
            <input
              type="text"
              placeholder="User Name"
              value={borrowFilters.userName}
              onChange={(e) =>
                setBorrowFilters({ ...borrowFilters, userName: e.target.value })
              }
              className="filter-input"
            />
            <input
              type="text"
              placeholder="Book Title"
              value={borrowFilters.bookTitle}
              onChange={(e) =>
                setBorrowFilters({
                  ...borrowFilters,
                  bookTitle: e.target.value,
                })
              }
              className="filter-input"
            />
            <select
              value={borrowFilters.status}
              onChange={(e) =>
                setBorrowFilters({ ...borrowFilters, status: e.target.value })
              }
              className="filter-select"
            >
              <option value="">All Statuses</option>
              <option value="Active">Active</option>
              <option value="PastDue">Past Due</option>
              <option value="Returned">Returned</option>
              <option value="ReturnedPastDue">Returned Past Due</option>
            </select>
            <label className="checkbox-label">
              <input
                type="checkbox"
                checked={borrowFilters.includeReturned}
                onChange={(e) =>
                  setBorrowFilters({
                    ...borrowFilters,
                    includeReturned: e.target.checked,
                  })
                }
              />
              Include Returned
            </label>
            <button onClick={fetchBorrows} className="apply-filters-btn">
              Apply Filters
            </button>
          </div>

          {/* Borrows Table */}
          <div className="table-container">
            <table className="data-table">
              <thead>
                <tr>
                  <th>ID</th>
                  <th>Book</th>
                  <th>User</th>
                  <th>Start Date</th>
                  <th>Due Date</th>
                  <th>Return Date</th>
                  <th>Status</th>
                  <th>Pending Request</th>
                  <th>Actions</th>
                </tr>
              </thead>
              <tbody>
                {borrows.map((borrow) => (
                  <tr key={borrow.id}>
                    <td>{borrow.id}</td>
                    <td>{borrow.bookTitle}</td>
                    <td>{borrow.userName}</td>
                    <td>{formatDate(borrow.startDate)}</td>
                    <td>{formatDate(borrow.dueDate)}</td>
                    <td>
                      {borrow.returnDate ? formatDate(borrow.returnDate) : "-"}
                    </td>
                    <td>
                      <span className={`status-badge status-${borrow.status}`}>
                        {
                          [
                            BorrowStatus.Active,
                            BorrowStatus.Returned,
                            BorrowStatus.ReturnedPastDue,
                            BorrowStatus.PastDue,
                          ][borrow.status]
                        }
                      </span>
                    </td>
                    <td>
                      {borrow.hasPendingReturnRequest ? (
                        <span className="pending-yes">Yes</span>
                      ) : (
                        <span className="pending-no">No</span>
                      )}
                    </td>
                    <td>
                      {canDirectReturn(borrow) && (
                        <button
                          onClick={() => handleDirectReturn(borrow.id)}
                          disabled={processingDirectReturn === borrow.id}
                          className="direct-return-btn"
                        >
                          {processingDirectReturn === borrow.id
                            ? "Processing..."
                            : "Direct Return"}
                        </button>
                      )}
                    </td>
                  </tr>
                ))}
                {borrows.length === 0 && !borrowsLoading && (
                  <tr>
                    <td colSpan={9} className="no-data">
                      No borrows found.
                    </td>
                  </tr>
                )}
              </tbody>
            </table>
          </div>
        </div>
      )}

      {activeTab === "requests" && (
        <div>
          {/* Return Request Filters */}
          <div className="filters-container">
            <input
              type="text"
              placeholder="User ID"
              value={requestFilters.userId}
              onChange={(e) =>
                setRequestFilters({ ...requestFilters, userId: e.target.value })
              }
              className="filter-input"
            />
            <input
              type="text"
              placeholder="User Name"
              value={requestFilters.userName}
              onChange={(e) =>
                setRequestFilters({
                  ...requestFilters,
                  userName: e.target.value,
                })
              }
              className="filter-input"
            />
            <select
              value={requestFilters.status}
              onChange={(e) =>
                setRequestFilters({ ...requestFilters, status: e.target.value })
              }
              className="filter-select"
            >
              <option value="">All Statuses</option>
              <option value="Pending">Pending</option>
              <option value="Accepted">Accepted</option>
            </select>
            <button onClick={fetchReturnRequests} className="apply-filters-btn">
              Apply Filters
            </button>
          </div>

          {/* Return Requests Table */}
          <div className="table-container">
            <table className="data-table">
              <thead>
                <tr>
                  <th>ID</th>
                  <th>Borrow ID</th>
                  <th>Book</th>
                  <th>User</th>
                  <th>Request Date</th>
                  <th>Due Date</th>
                  <th>Status</th>
                  <th>Overdue</th>
                  <th>Actions</th>
                </tr>
              </thead>
              <tbody>
                {returnRequests.map((request) => (
                  <tr
                    key={request.id}
                    className={request.isOverdue ? "overdue-row" : ""}
                  >
                    <td>{request.id}</td>
                    <td>{request.borrowId}</td>
                    <td>{request.bookTitle}</td>
                    <td>{request.userName}</td>
                    <td>{formatDate(request.requestDate)}</td>
                    <td>{formatDate(request.dueDate)}</td>
                    <td>
                      <span
                        className={`status-badge ${request.status === 0 ? "status-pending" : "status-accepted"}`}
                      >
                        {request.status === 0 ? "Pending" : "Accepted"}
                      </span>
                    </td>
                    <td>
                      {request.isOverdue ? (
                        <span className="overdue-yes">Yes</span>
                      ) : (
                        <span className="overdue-no">No</span>
                      )}
                    </td>
                    <td>
                      {request.status === 0 && (
                        <button
                          onClick={() => processReturnRequest(request.id, 1)}
                          disabled={processingRequestId === request.id}
                          className="accept-btn"
                        >
                          {processingRequestId === request.id
                            ? "Processing..."
                            : "Accept"}
                        </button>
                      )}
                    </td>
                  </tr>
                ))}
                {returnRequests.length === 0 && !requestsLoading && (
                  <tr>
                    <td colSpan={9} className="no-data">
                      No return requests found.
                    </td>
                  </tr>
                )}
              </tbody>
            </table>
          </div>
        </div>
      )}
    </div>
  );
}
