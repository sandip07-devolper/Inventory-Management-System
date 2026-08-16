/**
 * Renders numbered pagination controls into the given <ul> element.
 * `onPageChange(page)` is called when the user clicks a page/prev/next link.
 */
function renderPaginationControls(containerId, result, onPageChange) {
  const pagination = document.getElementById(containerId);
  pagination.innerHTML = "";

  if (result.totalPages <= 1) return;

  const addPageItem = (label, page, disabled, active) => {
    const li = document.createElement("li");
    li.className = `page-item ${disabled ? "disabled" : ""} ${active ? "active" : ""}`;
    const a = document.createElement("a");
    a.className = "page-link";
    a.href = "#";
    a.textContent = label;
    a.addEventListener("click", (e) => {
      e.preventDefault();
      if (disabled || active) return;
      onPageChange(page);
    });
    li.appendChild(a);
    pagination.appendChild(li);
  };

  addPageItem("«", result.pageNumber - 1, result.pageNumber === 1, false);

  for (let page = 1; page <= result.totalPages; page++) {
    addPageItem(String(page), page, false, page === result.pageNumber);
  }

  addPageItem("»", result.pageNumber + 1, result.pageNumber === result.totalPages, false);
}

function renderResultsSummaryText(elementId, result) {
  const summary = document.getElementById(elementId);
  if (!summary) return;

  if (result.totalCount === 0) {
    summary.textContent = "No results";
    return;
  }

  const start = (result.pageNumber - 1) * result.pageSize + 1;
  const end = Math.min(result.pageNumber * result.pageSize, result.totalCount);
  summary.textContent = `Showing ${start}–${end} of ${result.totalCount}`;
}
