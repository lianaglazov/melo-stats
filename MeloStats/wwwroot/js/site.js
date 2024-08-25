function toggleSidebar() {
    var sidebar = document.querySelector('.sidebar');
    var container = document.querySelector('.container');
    sidebar.classList.toggle('collapsed');
   
}

function fetchDataAndRedirect() {
    $('#initial-content').hide();
    $('#loading-screen').show();

    $.ajax({
        url: 'ListeningHistories/Stats',
        method: 'GET',
        success: function () {
            window.location.href = 'ListeningHistories/Stats';
        },
        error: function () {
            $('#loading-screen').hide();
            $('#initial-content').show();
            alert("An error occurred while fetching your data.");
        }
    });
}


