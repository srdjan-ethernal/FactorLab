window.factorLab = {
  downloadTextFile: function (fileName, contentType, content) {
    const blob = new Blob([content], { type: contentType || "text/plain" });
    const url = URL.createObjectURL(blob);
    const link = document.createElement("a");
    link.href = url;
    link.download = fileName || "factorlab-export.csv";
    document.body.appendChild(link);
    link.click();
    document.body.removeChild(link);
    URL.revokeObjectURL(url);
  }
};
