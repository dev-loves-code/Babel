import React, { useState, useEffect } from "react";
import axios from "axios";
import { useAuth } from "../../context/AuthContext";
import "./MyBorrowsPage.css";
import ReturnRequestsTable from "./ReturnRequestsTable";

const MyBorrowsPage: React.FC = () => {
  const { user } = useAuth();
  const [borrows, setBorrows] = useState<any[]>([]);
  const [returnRequests, setReturnRequests] = useState<any[]>([]);
  const [loading, setLoading] = useState(true);
  const [requestsLoading, setRequestsLoading] = useState(true);
  const [returnRequestLoading, setReturnRequestLoading] = useState<
    number | null
  >(null);

  const getAuthHeader = () => ({
    Authorization: user ? `Bearer ${user.token}` : "",
    "Content-Type": "application/json",
  });

  const statusLabels: Record<number, string> = {
    0: "Active",
    1: "Returned",
    2: "Returned Past Due",
    3: "Past Due",
  };

  useEffect(() => {
    const fetchData = async () => {
      if (!user) return;

      try {
        setLoading(true);
        setRequestsLoading(true);

        // Fetch both borrows and return requests in parallel
        const [borrowsRes, requestsRes] = await Promise.all([
          axios.get("http://localhost:5286/api/borrow/my-borrows", {
            headers: getAuthHeader(),
          }),
          axios.get("http://localhost:5286/api/ReturnRequest/my-requests", {
            headers: getAuthHeader(),
          }),
        ]);

        setBorrows(borrowsRes.data);
        setReturnRequests(requestsRes.data);
      } catch (err) {
        console.error("Failed to fetch data", err);
      } finally {
        setLoading(false);
        setRequestsLoading(false);
      }
    };

    fetchData();
  }, [user]);

  const handleReturnRequest = async (borrowId: number) => {
    if (!user) return;

    try {
      setReturnRequestLoading(borrowId);
      await axios.post(
        "http://localhost:5286/api/ReturnRequest",
        { borrowId },
        { headers: getAuthHeader() }
      );

      // Refresh the return requests to show the new one
      const requestsRes = await axios.get(
        "http://localhost:5286/api/ReturnRequest/my-requests",
        { headers: getAuthHeader() }
      );
      setReturnRequests(requestsRes.data);
    } catch (err: any) {
      console.error("Failed to create return request", err);
      alert(err.response?.data || "Failed to create return request");
    } finally {
      setReturnRequestLoading(null);
    }
  };

  // Check if a return request already exists for a borrow
  const hasReturnRequest = (borrowId: number) => {
    return returnRequests.some((request) => request.borrowId === borrowId);
  };

  if (!user) return <div>Please login to view your borrows.</div>;
  if (loading) return <div>Loading your borrows...</div>;

  return (
    <div className="my-borrows-page">
      <h1>My Borrows</h1>
      {borrows.length === 0 ? (
        <div className="no-borrows">You have no borrowed books.</div>
      ) : (
        <>
          <table>
            <thead>
              <tr>
                <th>Book Title</th>
                <th>Start Date</th>
                <th>Due Date</th>
                <th>Return Date</th>
                <th>Status</th>
                <th>Action</th>
              </tr>
            </thead>
            <tbody>
              {borrows.map((b) => {
                const hasRequest = hasReturnRequest(b.id);
                const canRequestReturn =
                  (b.status === 0 || b.status === 3) && !hasRequest;

                return (
                  <tr key={b.id}>
                    <td data-label="Book Title">{b.bookTitle}</td>
                    <td data-label="Start Date">
                      {new Date(b.startDate).toLocaleDateString()}
                    </td>
                    <td data-label="Due Date">
                      {new Date(b.dueDate).toLocaleDateString()}
                    </td>
                    <td data-label="Return Date">
                      {b.returnDate
                        ? new Date(b.returnDate).toLocaleDateString()
                        : "-"}
                    </td>
                    <td data-label="Status">
                      {statusLabels[b.status] ?? "Unknown"}
                    </td>
                    <td data-label="Action">
                      {canRequestReturn ? (
                        <button
                          className="return-request-btn"
                          onClick={() => handleReturnRequest(b.id)}
                          disabled={returnRequestLoading === b.id}
                        >
                          {returnRequestLoading === b.id
                            ? "Processing..."
                            : "Request Return"}
                        </button>
                      ) : hasRequest ? (
                        <span className="return-requested">
                          Return Requested
                        </span>
                      ) : (
                        <span className="no-action">-</span>
                      )}
                    </td>
                  </tr>
                );
              })}
            </tbody>
          </table>
        </>
      )}
      <ReturnRequestsTable
        returnRequests={returnRequests}
        loading={requestsLoading}
      />
    </div>
  );
};

export default MyBorrowsPage;
