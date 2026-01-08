//.card
//{
//border: none;
//    border - radius: 12px;
//}

//.product - img {
//width: 60px;
//height: 60px;
//    object-fit: cover;
//}

//.table td
//{
//    vertical-align: middle;
//    padding: 15px;
//}

///* Timeline */
//.timeline
//{
//position: relative;
//    padding - left: 0;
//}

//.timeline - item {
//position: relative;
//padding: 15px 0 15px 50px;
//color: #999;
//}

//.timeline - item i
//{
//position: absolute;
//left: 0;
//top: 15px;
//width: 35px;
//height: 35px;
//    border - radius: 50 %;
//background: #f0f0f0;
//    display: flex;
//    align - items: center;
//    justify - content: center;
//color: #999;
//    font - size: 14px;
//}

//.timeline - item::before {
//content: "";
//position: absolute;
//left: 17px;
//top: 50px;
//width: 2px;
//height: calc(100 % -35px);
//background: #e0e0e0;
//}

//.timeline - item:last - child::before {
//display: none;
//}

//.timeline - item.active {
//color: #333;
//    font - weight: 600;
//}

//.timeline - item.active i
//{
//background: linear - gradient(135deg, #667eea 0%, #764ba2 100%);
//    color: white;
//}

//.timeline - item.active::before {
//background: linear - gradient(180deg, #667eea 0%, #e0e0e0 100%);
//}
/* ========================================
   ORDER DETAILS PAGE STYLES
   ======================================== */

/* Product Image in Table */
.product - img {
width: 60px;
height: 60px;
object-fit: cover;
    border - radius: 8px;
}

/* Card Styling */
.card
{
border: none;
    border - radius: 12px;
transition: transform 0.3s, box - shadow 0.3s;
}

.card: hover {
transform: translateY(-5px);
    box - shadow: 0 10px 30px rgba(0, 0, 0, 0.15);
}

.card - header {
background: linear - gradient(135deg, #667eea 0%, #764ba2 100%);
    color: white;
    border - radius: 12px 12px 0 0!important;
padding: 15px 20px;
}

    .card - header h5
{
margin: 0;
    font - weight: 600;
}

.card - body {
padding: 20px;
}

/* Table Styling */
.table
{
    margin - bottom: 0;
}

    .table thead
{
    background: #f8f9fa;
    }

        .table thead th {
            font-weight: 600;
color: #495057;
            border - bottom: 2px solid #dee2e6;
        }

    .table td
{
    vertical-align: middle;
    padding: 15px;
}

    .table tbody tr:hover
{
background: #f8f9fa;
    }

/* Status Badge Styling */
.badge
{
padding: 8px 16px;
    border - radius: 20px;
    font - weight: 600;
    font - size: 14px;
}

.bg - warning {
background: linear - gradient(135deg, #f093fb 0%, #f5576c 100%) !important;
}

.bg - info {
background: linear - gradient(135deg, #4facfe 0%, #00f2fe 100%) !important;
}

.bg - primary {
background: linear - gradient(135deg, #667eea 0%, #764ba2 100%) !important;
}

.bg - success {
background: linear - gradient(135deg, #11998e 0%, #38ef7d 100%) !important;
}

.bg - danger {
background: linear - gradient(135deg, #eb3349 0%, #f45c43 100%) !important;
}

.bg - secondary {
background: linear - gradient(135deg, #bdc3c7 0%, #2c3e50 100%) !important;
}

/* Timeline Styling */
.timeline
{
position: relative;
    padding - left: 0;
    list - style: none;
}

.timeline - item {
position: relative;
padding: 15px 0 15px 50px;
color: #999;
    transition: all 0.3s;
}

    .timeline - item i
{
position: absolute;
left: 0;
top: 15px;
width: 35px;
height: 35px;
    border - radius: 50 %;
background: #f0f0f0;
        display: flex;
    align - items: center;
    justify - content: center;
color: #999;
        font - size: 14px;
transition: all 0.3s;
}

    .timeline - item::before {
content: "";
position: absolute;
left: 17px;
top: 50px;
width: 2px;
height: calc(100 % -35px);
background: #e0e0e0;
    }

    .timeline - item:last - child::before {
display: none;
}

    .timeline - item.active {
color: #333;
        font - weight: 600;
}

        .timeline - item.active i
{
background: linear - gradient(135deg, #667eea 0%, #764ba2 100%);
            color: white;
transform: scale(1.1);
    box - shadow: 0 4px 10px rgba(102, 126, 234, 0.4);
}

        .timeline - item.active::before {
background: linear - gradient(180deg, #667eea 0%, #e0e0e0 100%);
        }

/* Order Summary Sticky */
.sticky - top {
position: sticky;
top: 20px;
    z - index: 100;
}

/* Back Button */
.btn - outline - secondary {
    border - radius: 8px;
padding: 10px 20px;
    font - weight: 500;
transition: all 0.3s;
}

    .btn - outline - secondary:hover
{
transform: translateY(-2px);
    box - shadow: 0 5px 15px rgba(0, 0, 0, 0.2);
}

/* Responsive Design */
@media(max - width: 768px) {
    .product - img {
    width: 50px;
    height: 50px;
    }

    .table - responsive {
        font - size: 14px;
    }

    .timeline - item {
        padding - left: 40px;
        font - size: 14px;
    }

        .timeline - item i {
    width: 30px;
    height: 30px;
        font - size: 12px;
    }

        .timeline - item::before {
    left: 14px;
    }

    .sticky - top {
    position: relative;
    top: 0;
    }
}

/* Loading Animation */
@keyframes fadeIn
{
    from
    {
    opacity: 0;
    transform: translateY(20px);
    }
    to
    {
    opacity: 1;
    transform: translateY(0);
    }
}

.card
{
animation: fadeIn 0.5s ease-out;
}

/* Print Styles */
@media print
{
    .btn, .timeline
    {
    display: none;
    }

    .card
    {
        box - shadow: none;
    border: 1px solid #dee2e6;
    }
}