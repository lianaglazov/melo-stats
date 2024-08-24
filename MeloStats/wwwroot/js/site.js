function toggleSidebar() {
    var sidebar = document.querySelector('.sidebar');
    var container = document.querySelector('.container');
    sidebar.classList.toggle('collapsed');
    //container.style.marginLeft = sidebar.classList.contains('collapsed') ? '0px' : '250px';
}
