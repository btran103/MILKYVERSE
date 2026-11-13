import sqlite3
import json
import csv
import os
import re
import sys
import zipfile

sys.stdout.reconfigure(encoding='utf-8')

DB_PATH = 'backend/MilkyVerse.Api/milkyverse_local.db'
OUTPUT_DIRS = [
    'dataset_tra_sua/tra_sua_dataset',
    'dataset'
]

for d in OUTPUT_DIRS:
    os.makedirs(d, exist_ok=True)

conn = sqlite3.connect(DB_PATH)
cursor = conn.cursor()

# 1. Fetch Categories
cursor.execute('SELECT Id, Name, Slug FROM Categories ORDER BY Id')
categories = cursor.fetchall()
cat_map = {c[0]: {'name': c[1], 'slug': c[2]} for c in categories}

# Helper for slugifying
def slugify(text):
    text = text.lower()
    mapping = {
        'à':'a','á':'a','ả':'a','ã':'a','ạ':'a','ă':'a','ằ':'a','ắ':'a','ẳ':'a','ẵ':'a','ặ':'a','â':'a','ầ':'a','ấ':'a','ẩ':'a','ẫ':'a','ậ':'a',
        'è':'e','é':'e','ẻ':'e','ẽ':'e','ẹ':'e','ê':'e','ề':'e','ế':'e','ể':'e','ễ':'e','ệ':'e',
        'ì':'i','í':'i','ỉ':'i','ĩ':'i','ị':'i',
        'ò':'o','ó':'o','ỏ':'o','õ':'o','ọ':'o','ô':'o','ồ':'o','ố':'o','ổ':'o','ỗ':'o','ộ':'o','ơ':'o','ờ':'o','ớ':'o','ở':'o','ỡ':'o','ợ':'o',
        'ù':'u','ú':'u','ủ':'u','ũ':'u','ụ':'u','ư':'u','ừ':'u','ứ':'u','ử':'u','ữ':'u','ự':'u',
        'ỳ':'y','ý':'y','ỷ':'y','ỹ':'y','ỵ':'y',
        'đ':'d'
    }
    for k, v in mapping.items():
        text = text.replace(k, v)
    text = re.sub(r'[^a-z0-9]+', '_', text).strip('_')
    return text

# 2. Fetch Toppings
cursor.execute('SELECT Id, Name, Price, IsAvailable FROM Toppings ORDER BY Id')
raw_toppings = cursor.fetchall()

toppings_list = []
for t in raw_toppings:
    tid = slugify(t[1])
    tags = ['topping']
    name_lower = t[1].lower()
    if 'trân châu' in name_lower:
        tags.extend(['dai', 'ngot'])
    if 'thạch' in name_lower or 'sương sáo' in name_lower:
        tags.extend(['gion', 'thanh'])
    if 'kem' in name_lower or 'pudding' in name_lower or 'flan' in name_lower:
        tags.extend(['beo', 'mem'])
    if 'phô mai' in name_lower or 'trứng' in name_lower or 'xúc xích' in name_lower:
        tags.append('topping_an_vat')

    toppings_list.append({
        'id': tid,
        'code': f'TOP-{t[0]:02d}',
        'name_vi': t[1],
        'price': int(t[2]),
        'tags': list(set(tags)),
        'available': bool(t[3])
    })

allowed_top_ids = [t['id'] for t in toppings_list]

# 3. Fetch Products
cursor.execute('''
    SELECT Id, CategoryId, Name, Slug, Description, BasePrice, IsBestSeller, IsNew, IsActive, ProductType 
    FROM Products ORDER BY Id
''')
raw_products = cursor.fetchall()

menu_list = []
for p in raw_products:
    pid = f'MV-{p[0]:03d}'
    cat = cat_map.get(p[1], {'name': 'Khác', 'slug': 'khac'})
    name_vi = p[2]
    slug = p[3]
    desc = p[4] or ''
    base_price = int(p[5])
    is_best_seller = bool(p[6])
    is_new = bool(p[7])
    is_active = bool(p[8])
    prod_type = p[9] or 'DRINK'
    cat_slug = cat['slug']

    is_milk_tea = ('tra-sua' in cat_slug or 'oolong-rang' in cat_slug)
    has_milk = is_milk_tea or ('sua-tuoi' in cat_slug or 'macchiato' in cat_slug or 'frappe' in cat_slug or 'matcha-cacao' in cat_slug)
    has_caffeine = ('tra' in cat_slug or 'ca-phe' in cat_slug or 'matcha' in cat_slug) and not ('nuoc-ep' in cat_slug or 'nuoc-chanh' in cat_slug or 'soda' in cat_slug)

    if prod_type == 'DRINK':
        sugar_options = [0, 30, 50, 70, 100]
        ice_options = [0, 30, 50, 70, 100]
        if 'nuoc-ep' in cat_slug:
            default_sugar = 0
            has_milk = False
            has_caffeine = False
        elif 'nguyen-chat' in cat_slug:
            default_sugar = 0
            has_milk = False
        else:
            default_sugar = 50
        sizes = {'M': base_price, 'L': base_price + 8000}
        hot_avail = bool(is_milk_tea or 'ca-phe' in cat_slug or 'nguyen-chat' in cat_slug)
    else:
        sugar_options = []
        ice_options = []
        default_sugar = 0
        has_milk = False
        has_caffeine = False
        sizes = {'Tiêu chuẩn': base_price}
        hot_avail = True

    tags = []
    if is_best_seller:
        tags.append('best_seller')
    if is_new:
        tags.append('mon_moi')
    if is_milk_tea:
        tags.extend(['tra_sua', 'beo', 'thom_tra'])
    if 'tra-trai-cay' in cat_slug or 'nuoc-ep' in cat_slug:
        tags.extend(['trai_cay', 'thanh_mat', 'giai_nhiet'])
    if 'ca-phe' in cat_slug:
        tags.extend(['ca_phe', 'tinh_tao', 'dam_vi'])
    if prod_type == 'FOOD':
        tags.extend(['an_vat', 'nong_hoi', 'thom_ngon'])
    if not has_caffeine and prod_type == 'DRINK':
        tags.append('khong_caffeine')
    if default_sugar == 0:
        tags.append('khong_duong')
    elif default_sugar <= 30:
        tags.append('it_ngot')

    def_toppings = []
    n_lower = name_vi.lower()
    if 'trân châu đường đen' in n_lower:
        def_toppings.append('tran_chau_duong_den')
    elif 'trân châu hoàng kim' in n_lower:
        def_toppings.append('tran_chau_hoang_kim')
    elif 'trân châu trắng' in n_lower:
        def_toppings.append('tran_chau_trang')
    elif 'trân châu đen' in n_lower:
        def_toppings.append('tran_chau_den')
    elif 'kem cheese' in n_lower or 'macchiato' in n_lower:
        def_toppings.append('kem_cheese')

    menu_list.append({
        'id': pid,
        'db_id': p[0],
        'slug': slug,
        'name_vi': name_vi,
        'category': cat_slug,
        'category_name': cat['name'],
        'product_type': prod_type,
        'is_milk_tea': is_milk_tea,
        'has_milk': has_milk,
        'has_caffeine': has_caffeine,
        'default_sugar_percent': default_sugar,
        'sugar_options': sugar_options,
        'ice_options': ice_options,
        'sizes': sizes,
        'tags': list(set(tags)),
        'description': desc,
        'default_toppings': def_toppings,
        'allowed_toppings': allowed_top_ids if prod_type == 'DRINK' else ['them_pho_mai_keo_soi_mon_an', 'them_trung_op_la_long_dao', 'them_xuc_xich_duc_chien'],
        'hot_available': hot_avail,
        'available': is_active,
        'product_url': f'http://localhost:3000/menu/{slug}',
        'order_url': f'http://localhost:3000/checkout?item={p[0]}&size=M'
    })

# 4. Custom Builder JSON
custom_builder = {
    'builder_url': 'http://localhost:3000/diy',
    'steps': [
        'base',
        'milk',
        'flavor',
        'sugar_percent',
        'ice_percent',
        'toppings',
        'size'
    ],
    'base': [
        {'id': 'tra_den', 'name_vi': 'Cốt Hồng Trà Ceylon Hảo Hạng', 'caffeine': 1, 'price': 25000},
        {'id': 'tra_oolong', 'name_vi': 'Cốt Ô Long Rang Khói', 'caffeine': 1, 'price': 28000},
        {'id': 'tra_lai', 'name_vi': 'Cốt Lục Trà Hoa Lài Thanh Khiết', 'caffeine': 1, 'price': 25000},
        {'id': 'tra_matcha', 'name_vi': 'Matcha Uji Nhật Bản', 'caffeine': 1, 'price': 30000},
        {'id': 'khong_tra', 'name_vi': 'Không Dùng Trà (Dành Cho Trẻ Em/Bà Bầu)', 'caffeine': 0, 'price': 20000}
    ],
    'milk': [
        {'id': 'sua_tuoi_dalat', 'name_vi': 'Sữa Tươi Thanh Trùng Dalat Milk', 'price': 12000},
        {'id': 'sua_dac', 'name_vi': 'Sữa Đặc Truyền Thống Ngọt Béo', 'price': 8000},
        {'id': 'sua_hanh_nhan', 'name_vi': 'Sữa Hạt Hạnh Nhân Thực Vật', 'price': 15000},
        {'id': 'sua_oat', 'name_vi': 'Sữa Yến Mạch Oat Milk', 'price': 15000},
        {'id': 'khong_sua', 'name_vi': 'Không Dùng Sữa (Uống Trà Trái Cây/Mộc)', 'price': 0}
    ],
    'flavor': [
        {'id': 'khong_vi', 'name_vi': 'Nguyên Bản Thuần Vị', 'price': 0},
        {'id': 'dao', 'name_vi': 'Đào Tươi Hoàng Kim', 'price': 8000},
        {'id': 'vai', 'name_vi': 'Vải Thiều Nhiệt Đới', 'price': 8000},
        {'id': 'dau_tay', 'name_vi': 'Dâu Tây Đà Lạt Mọng Nước', 'price': 8000},
        {'id': 'chanh_day', 'name_vi': 'Chanh Dây Tươi Chua Ngọt', 'price': 8000},
        {'id': 'khoai_mon', 'name_vi': 'Khoai Môn Bùi Béo Sánh Mịn', 'price': 10000},
        {'id': 'socola', 'name_vi': 'Socola Đen Thượng Hạng', 'price': 10000}
    ],
    'sugar_options': [0, 30, 50, 70, 100],
    'ice_options': [0, 30, 50, 70, 100],
    'toppings': [
        {'id': t['id'], 'name_vi': t['name_vi'], 'price': t['price']} 
        for t in toppings_list if not 'topping_an_vat' in t['tags']
    ],
    'sizes': {'M': 0, 'L': 8000},
    'combo_suggestions': [
        {
            'name': 'Trà Sữa Oolong Kem Cheese Signature',
            'desc': 'Trà oolong đậm vị, sữa tươi Dalat, kem cheese béo mặn nhẹ',
            'link': 'http://localhost:3000/diy?base=tra_oolong&milk=sua_tuoi_dalat&flavor=khong_vi&toppings=kem_cheese,tran_chau_hoang_kim&sugar=50&ice=70&size=M'
        },
        {
            'name': 'Lục Trà Lài Vải Thiều Thạch Trắng',
            'desc': 'Thanh mát giải nhiệt ngày hè, không béo, ít calo',
            'link': 'http://localhost:3000/diy?base=tra_lai&milk=khong_sua&flavor=vai&toppings=tran_chau_trang,thach_nha_dam&sugar=30&ice=100&size=L'
        },
        {
            'name': 'Matcha Sữa Yến Mạch Healthy',
            'desc': 'Dành riêng cho team ăn kiêng và thuần chay',
            'link': 'http://localhost:3000/diy?base=tra_matcha&milk=sua_oat&flavor=khong_vi&sugar=30&ice=50&size=M'
        }
    ]
}

# 5. Orders Sample JSON
orders_sample = {
    'orders': [
        {
            'order_id': 'MV-DH1001',
            'user_id': 'u001',
            'status': 'pending',
            'created_at': '2026-11-07T09:15:00+07:00',
            'items': [
                {
                    'item_id': 'MV-006',
                    'name_vi': 'Trà sữa trân châu đường đen',
                    'size': 'L',
                    'sugar_percent': 50,
                    'ice_percent': 70,
                    'toppings': ['tran_chau_duong_den'],
                    'qty': 1,
                    'price': 58000
                }
            ],
            'total': 58000,
            'payment': 'momo',
            'tracking_url': 'http://localhost:3000/orders'
        },
        {
            'order_id': 'MV-DH1002',
            'user_id': 'u001',
            'status': 'delivering',
            'created_at': '2026-11-07T08:00:00+07:00',
            'items': [
                {
                    'item_id': 'MV-027',
                    'name_vi': 'Trà đào cam sả',
                    'size': 'M',
                    'sugar_percent': 50,
                    'ice_percent': 100,
                    'toppings': ['thach_nha_dam'],
                    'qty': 2,
                    'price': 90000
                }
            ],
            'total': 90000,
            'payment': 'cod',
            'tracking_url': 'http://localhost:3000/orders'
        }
    ],
    'cancellation_policy': {
        'allowed_statuses': ['pending'],
        'time_limit_minutes': 10,
        'require_confirmation': True,
        'user_match_required': True
    }
}

# 6. Tools JSON
tools = [
    {
        'type': 'function',
        'function': {
            'name': 'search_menu',
            'description': 'Tìm/lọc món trên thực đơn MilkyVerse theo từ khoá, danh mục, ngân sách, độ ngọt, tiêu chuẩn ăn kiêng (không caffeine, không sữa, đồ ăn vặt).',
            'parameters': {
                'type': 'object',
                'properties': {
                    'query': {
                        'type': 'string',
                        'description': 'Từ khoá tên món hoặc thành phần (ví dụ: trân châu, đào, khoai môn, cá viên)'
                    },
                    'categories': {
                        'type': 'array',
                        'items': {'type': 'string'},
                        'description': 'tra-sua-truyen-thong|tra-trai-cay|tra-nguyen-chat-pha-vi|tra-macchiato-kem-cheese|sua-tuoi-do-uong-sua|matcha-cacao|frappe-da-xay|nuoc-ep-trai-cay|soda-do-uong-co-gas|nuoc-chanh-giai-khat|ca-phe-do-uong-bo-sung|tra-sua-oolong-rang|vien-chien|ga-chan-ga|khoai-tay-khoai-lang|pho-mai-xuc-xich-do-chien|banh-trang-mon-tron|trung-chen-mon-trung|mi-mon-cay|com-mon-no|noi-tang-mon-nuong|an-vat-bo-sung|combo-an-uong'
                    },
                    'exclude_categories': {
                        'type': 'array',
                        'items': {'type': 'string'}
                    },
                    'exclude_milk_tea': {
                        'type': 'boolean',
                        'description': 'true nếu người dùng KHÔNG muốn uống trà sữa'
                    },
                    'no_milk': {
                        'type': 'boolean',
                        'description': 'true nếu không dùng sữa (dị ứng lactose hoặc thuần chay)'
                    },
                    'no_caffeine': {
                        'type': 'boolean',
                        'description': 'true nếu tránh trà và cà phê (cho buổi tối hoặc mẹ bầu)'
                    },
                    'product_type': {
                        'type': 'string',
                        'enum': ['DRINK', 'FOOD'],
                        'description': 'DRINK: Đồ uống, FOOD: Đồ ăn vặt'
                    },
                    'max_default_sugar_percent': {
                        'type': 'integer',
                        'description': 'Độ ngọt tối đa mặc định (ví dụ: 0 cho không đường, 30 cho ít ngọt)'
                    },
                    'max_price': {
                        'type': 'integer',
                        'description': 'Ngân sách tối đa tính theo VNĐ'
                    },
                    'include_tags': {
                        'type': 'array',
                        'items': {'type': 'string'}
                    },
                    'exclude_tags': {
                        'type': 'array',
                        'items': {'type': 'string'}
                    },
                    'limit': {
                        'type': 'integer',
                        'default': 5
                    }
                },
                'required': []
            }
        }
    },
    {
        'type': 'function',
        'function': {
            'name': 'get_item_link',
            'description': 'Lấy thông tin chi tiết món, link trang món và nút Mua Ngay trực tiếp chuyển sang checkout.',
            'parameters': {
                'type': 'object',
                'properties': {
                    'item_id': {
                        'type': 'string',
                        'description': 'Mã món (ví dụ: MV-006 hoặc ID số 6)'
                    },
                    'size': {
                        'type': 'string',
                        'enum': ['M', 'L', 'Tiêu chuẩn']
                    },
                    'sugar_percent': {
                        'type': 'integer',
                        'enum': [0, 30, 50, 70, 100]
                    },
                    'ice_percent': {
                        'type': 'integer',
                        'enum': [0, 30, 50, 70, 100]
                    },
                    'toppings': {
                        'type': 'array',
                        'items': {'type': 'string'}
                    }
                },
                'required': ['item_id']
            }
        }
    },
    {
        'type': 'function',
        'function': {
            'name': 'create_order',
            'description': 'Tạo đơn hàng chính thức sau khi khách đã xác nhận món, size, topping và hình thức thanh toán.',
            'parameters': {
                'type': 'object',
                'properties': {
                    'user_id': {
                        'type': 'string'
                    },
                    'items': {
                        'type': 'array',
                        'items': {'type': 'object'}
                    },
                    'payment': {
                        'type': 'string',
                        'enum': ['momo', 'cod', 'bank', 'vnpay']
                    },
                    'note': {
                        'type': 'string'
                    }
                },
                'required': ['user_id', 'items']
            }
        }
    },
    {
        'type': 'function',
        'function': {
            'name': 'list_orders',
            'description': 'Liệt kê danh sách đơn hàng của khách hàng theo trạng thái để tra cứu hoặc chọn huỷ.',
            'parameters': {
                'type': 'object',
                'properties': {
                    'user_id': {
                        'type': 'string'
                    },
                    'status': {
                        'type': 'string',
                        'enum': ['pending', 'delivering', 'completed', 'cancelled']
                    }
                },
                'required': ['user_id']
            }
        }
    },
    {
        'type': 'function',
        'function': {
            'name': 'cancel_order',
            'description': 'Huỷ đơn hàng đang chờ xử lý (pending). Bắt buộc phải có sự xác nhận đồng ý của người dùng trước khi gọi.',
            'parameters': {
                'type': 'object',
                'properties': {
                    'user_id': {
                        'type': 'string'
                    },
                    'order_id': {
                        'type': 'string'
                    },
                    'reason': {
                        'type': 'string'
                    }
                },
                'required': ['user_id', 'order_id']
            }
        }
    },
    {
        'type': 'function',
        'function': {
            'name': 'get_custom_builder_options',
            'description': 'Lấy danh sách các bước, nguyên liệu tự pha chế và combo gợi ý cho trang Tự Pha Chế /diy.',
            'parameters': {
                'type': 'object',
                'properties': {
                    'preference_tags': {
                        'type': 'array',
                        'items': {'type': 'string'}
                    }
                },
                'required': []
            }
        }
    },
    {
        'type': 'function',
        'function': {
            'name': 'build_custom_drink_link',
            'description': 'Tạo đường link tự pha chế đã điền sẵn các tuỳ chọn tại /diy.',
            'parameters': {
                'type': 'object',
                'properties': {
                    'base': {'type': 'string'},
                    'milk': {'type': 'string'},
                    'flavor': {'type': 'string'},
                    'sugar_percent': {'type': 'integer'},
                    'ice_percent': {'type': 'integer'},
                    'toppings': {
                        'type': 'array',
                        'items': {'type': 'string'}
                    }
                },
                'required': ['base']
            }
        }
    }
]

# 7. Conversations JSONL (16 Scenarios updated for MilkyVerse)
SYS_PROMPT = "Bạn là Milky - AI Barista thông minh của hệ thống MilkyVerse. Bạn có thể tìm món, gợi ý đồ uống & đồ ăn vặt theo sở thích, gửi link Mua Ngay, tạo đơn, kiểm tra tiến độ đơn hàng và hướng dẫn tự pha chế tại /diy. Luôn tôn trọng bộ lọc của người dùng (ví dụ: 'không trà sữa' => exclude_milk_tea=true). Luôn xác nhận trước khi tạo hoặc huỷ đơn. Chỉ huỷ đơn 'pending' của đúng người dùng. Trả lời tiếng Việt thân thiện, chuẩn vị MilkyVerse. [user_id=u001]"

convs = [
    # 1. Direct item order
    {
        'messages': [
            {'role': 'system', 'content': SYS_PROMPT},
            {'role': 'user', 'content': 'đặt cho mình 1 ly trà sữa trân châu đường đen'},
            {'role': 'assistant', 'content': '', 'tool_calls': [{'name': 'get_item_link', 'arguments': {'item_id': 'MV-006', 'size': 'M'}}]},
            {'role': 'tool', 'name': 'get_item_link', 'content': json.dumps({
                'item_id': 'MV-006',
                'name_vi': 'Trà sữa trân châu đường đen',
                'size': 'M',
                'price': 50000,
                'default_sugar_percent': 50,
                'default_ice_percent': 70,
                'product_url': 'http://localhost:3000/menu/tra-sua-tran-chau-duong-den',
                'order_url': 'http://localhost:3000/checkout?item=6&size=M'
            }, ensure_ascii=False)},
            {'role': 'assistant', 'content': 'Dạ Milky gửi bạn thông tin món **Trà sữa trân châu đường đen** size M (50.000đ). Mặc định là 50% đường, 70% đá chuẩn vị MilkyVerse ạ. Bạn có muốn đổi size L (+8.000đ), chỉnh lượng đường đá hay bấm [⚡ Mua Ngay](http://localhost:3000/checkout?item=6&size=M) luôn không ạ?'}
        ]
    },
    # 2. Detailed order with payment
    {
        'messages': [
            {'role': 'system', 'content': SYS_PROMPT},
            {'role': 'user', 'content': 'Cho mình 1 trà sữa trân châu đường đen size L, 50% đường, đá 70%, trả bằng momo'},
            {'role': 'assistant', 'content': 'Dạ Milky xác nhận đơn của bạn gồm:\n- 1x **Trà sữa trân châu đường đen** (Size L, 50% đường, 70% đá, trân châu đường đen): 58.000đ\n- Thanh toán: MoMo\nBạn xác nhận đặt đơn này nhé?'},
            {'role': 'user', 'content': 'ok đặt luôn'},
            {'role': 'assistant', 'content': '', 'tool_calls': [{'name': 'create_order', 'arguments': {
                'user_id': 'u001',
                'items': [{'item_id': 'MV-006', 'size': 'L', 'sugar_percent': 50, 'ice_percent': 70, 'toppings': ['tran_chau_duong_den'], 'qty': 1}],
                'payment': 'momo'
            }}]},
            {'role': 'tool', 'name': 'create_order', 'content': json.dumps({'order_id': 'MV-DH1001', 'status': 'pending', 'total': 58000, 'payment_url': 'https://momo.vn/pay/MV-DH1001', 'tracking_url': 'http://localhost:3000/orders'}, ensure_ascii=False)},
            {'role': 'assistant', 'content': 'Đơn hàng **MV-DH1001** đã được tạo thành công! Tổng cộng là 58.000đ. Bạn có thể thanh toán qua MoMo và theo dõi tiến trình pha chế tại [Trang Đơn Hàng](http://localhost:3000/orders) nha!'}
        ]
    },
    # 3. Cancel pending order
    {
        'messages': [
            {'role': 'system', 'content': SYS_PROMPT},
            {'role': 'user', 'content': 'huỷ đơn trà sữa mình mới đặt'},
            {'role': 'assistant', 'content': '', 'tool_calls': [{'name': 'list_orders', 'arguments': {'user_id': 'u001', 'status': 'pending'}}]},
            {'role': 'tool', 'name': 'list_orders', 'content': json.dumps({'orders': [{'order_id': 'MV-DH1001', 'name': 'Trà sữa trân châu đường đen', 'total': 58000, 'status': 'pending'}]}, ensure_ascii=False)},
            {'role': 'assistant', 'content': 'Milky tìm thấy đơn hàng **MV-DH1001** (Trà sữa trân châu đường đen - 58.000đ) đang ở trạng thái Chờ xử lý. Bạn có chắc chắn muốn huỷ đơn này không ạ?'},
            {'role': 'user', 'content': 'ừ huỷ giúp mình'},
            {'role': 'assistant', 'content': '', 'tool_calls': [{'name': 'cancel_order', 'arguments': {'user_id': 'u001', 'order_id': 'MV-DH1001', 'reason': 'Người dùng yêu cầu huỷ'}}]},
            {'role': 'tool', 'name': 'cancel_order', 'content': json.dumps({'success': True, 'order_id': 'MV-DH1001', 'status': 'cancelled'}, ensure_ascii=False)},
            {'role': 'assistant', 'content': 'Dạ đơn hàng **MV-DH1001** đã được huỷ thành công rồi ạ. Khi nào muốn thưởng thức trà sữa bạn cứ nhắn Milky nhé!'}
        ]
    },
    # 4. Cancel order by item name
    {
        'messages': [
            {'role': 'system', 'content': SYS_PROMPT},
            {'role': 'user', 'content': 'huỷ đơn trà đào của mình giúp'},
            {'role': 'assistant', 'content': '', 'tool_calls': [{'name': 'list_orders', 'arguments': {'user_id': 'u001', 'status': 'pending'}}]},
            {'role': 'tool', 'name': 'list_orders', 'content': json.dumps({'orders': []}, ensure_ascii=False)},
            {'role': 'assistant', 'content': 'Milky kiểm tra thì hiện tại bạn không có đơn trà đào nào ở trạng thái Chờ xử lý (pending) cả. Các đơn đang giao hoặc đã hoàn tất sẽ không thể huỷ trên hệ thống được bạn nha.'}
        ]
    },
    # 5. Cancel by specific order ID
    {
        'messages': [
            {'role': 'system', 'content': SYS_PROMPT},
            {'role': 'user', 'content': 'huỷ đơn MV-DH1004 giúp mình'},
            {'role': 'assistant', 'content': 'Bạn xác nhận chắc chắn muốn huỷ đơn hàng mã **MV-DH1004** đúng không ạ?'},
            {'role': 'user', 'content': 'đúng rồi'},
            {'role': 'assistant', 'content': '', 'tool_calls': [{'name': 'cancel_order', 'arguments': {'user_id': 'u001', 'order_id': 'MV-DH1004', 'reason': 'Khách huỷ qua mã đơn'}}]},
            {'role': 'tool', 'name': 'cancel_order', 'content': json.dumps({'success': True, 'order_id': 'MV-DH1004', 'status': 'cancelled'}, ensure_ascii=False)},
            {'role': 'assistant', 'content': 'Đã hoàn tất huỷ đơn **MV-DH1004** cho bạn rồi nha!'}
        ]
    },
    # 6. Preference: low sugar, non-milk tea
    {
        'messages': [
            {'role': 'system', 'content': SYS_PROMPT},
            {'role': 'user', 'content': 'mình muốn uống món ít ngọt thôi, không phải trà sữa'},
            {'role': 'assistant', 'content': '', 'tool_calls': [{'name': 'search_menu', 'arguments': {'exclude_milk_tea': True, 'max_default_sugar_percent': 30}}]},
            {'role': 'tool', 'name': 'search_menu', 'content': json.dumps([
                {'id': 'MV-044', 'name_vi': 'Trà Xanh Hoa Lài', 'category': 'tra-nguyen-chat-pha-vi', 'price': 35000, 'default_sugar_percent': 0, 'product_url': 'http://localhost:3000/menu/tra-xanh-hoa-lai'},
                {'id': 'MV-043', 'name_vi': 'Trà Oolong Nguyên Chất', 'category': 'tra-nguyen-chat-pha-vi', 'price': 38000, 'default_sugar_percent': 0, 'product_url': 'http://localhost:3000/menu/tra-oolong-nguyen-chat'},
                {'id': 'MV-115', 'name_vi': 'Nước Ép Táo Cà Rốt', 'category': 'nuoc-ep-trai-cay', 'price': 42000, 'default_sugar_percent': 0, 'product_url': 'http://localhost:3000/menu/nuoc-ep-tao-ca-rot'}
            ], ensure_ascii=False)},
            {'role': 'assistant', 'content': 'Milky gợi ý cho bạn 3 món thanh mát, chuẩn ít ngọt và hoàn toàn không phải trà sữa:\n1. 🍵 [Trà Xanh Hoa Lài](http://localhost:3000/menu/tra-xanh-hoa-lai) (35.000đ) - Hương lài thanh tao, 0% đường mộc tự nhiên.\n2. 🍃 [Trà Oolong Nguyên Chất](http://localhost:3000/menu/tra-oolong-nguyen-chat) (38.000đ) - Cốt Ô Long đậm đà hậu ngọt.\n3. 🍎 [Nước Ép Táo Cà Rốt](http://localhost:3000/menu/nuoc-ep-tao-ca-rot) (42.000đ) - Ép tươi nguyên chất, giàu vitamin.\nBạn muốn chọn món nào để Milky chuẩn bị nhé?'}
        ]
    },
    # 7. Preference: night time, no caffeine, no milk tea
    {
        'messages': [
            {'role': 'system', 'content': SYS_PROMPT},
            {'role': 'user', 'content': 'tối rồi, gợi ý món không caffeine và không trà sữa nha'},
            {'role': 'assistant', 'content': '', 'tool_calls': [{'name': 'search_menu', 'arguments': {'exclude_milk_tea': True, 'no_caffeine': True}}]},
            {'role': 'tool', 'name': 'search_menu', 'content': json.dumps([
                {'id': 'MV-114', 'name_vi': 'Nước Ép Dưa Hấu', 'category': 'nuoc-ep-trai-cay', 'price': 38000, 'product_url': 'http://localhost:3000/menu/nuoc-ep-dua-hau'},
                {'id': 'MV-121', 'name_vi': 'Soda Việt Quất Bạc Hà', 'category': 'soda-do-uong-co-gas', 'price': 35000, 'product_url': 'http://localhost:3000/menu/soda-viet-quat'},
                {'id': 'MV-061', 'name_vi': 'Sữa Tươi Trân Châu Đường Đen', 'category': 'sua-tuoi-do-uong-sua', 'price': 45000, 'product_url': 'http://localhost:3000/menu/sua-tuoi-tran-chau-duong-den'}
            ], ensure_ascii=False)},
            {'role': 'assistant', 'content': 'Uống buổi tối không lo mất ngủ thì Milky có các món thơm ngon không chứa caffeine này nha:\n- 🍉 [Nước Ép Dưa Hấu](http://localhost:3000/menu/nuoc-ep-dua-hau) (38.000đ): Thanh mát, mát ruột dễ ngủ.\n- 🫐 [Soda Việt Quất Bạc Hà](http://localhost:3000/menu/soda-viet-quat) (35.000đ): Sảng khoái giải nhiệt.\n- 🥛 [Sữa Tươi Trân Châu Đường Đen](http://localhost:3000/menu/sua-tuoi-tran-chau-duong-den) (45.000đ): Sữa tươi Dalat béo ngậy kèm trân châu dẻo nóng, hoàn toàn không có cốt trà.'}
        ]
    },
    # 8. Preference: fruit, cool, under 40k
    {
        'messages': [
            {'role': 'system', 'content': SYS_PROMPT},
            {'role': 'user', 'content': 'có món nào trái cây mát mát dưới 40k không'},
            {'role': 'assistant', 'content': '', 'tool_calls': [{'name': 'search_menu', 'arguments': {'categories': ['tra-trai-cay', 'nuoc-ep-trai-cay'], 'max_price': 40000}}]},
            {'role': 'tool', 'name': 'search_menu', 'content': json.dumps([
                {'id': 'MV-027', 'name_vi': 'Trà Đào Cam Sả', 'price': 39000, 'product_url': 'http://localhost:3000/menu/tra-dao-cam-sa'},
                {'id': 'MV-028', 'name_vi': 'Trà Tắc Xí Muội Mật Ong', 'price': 32000, 'product_url': 'http://localhost:3000/menu/tra-tac-xi-muoi'},
                {'id': 'MV-114', 'name_vi': 'Nước Ép Dưa Hấu', 'price': 38000, 'product_url': 'http://localhost:3000/menu/nuoc-ep-dua-hau'}
            ], ensure_ascii=False)},
            {'role': 'assistant', 'content': 'Dạ có ngay các món trái cây mát lạnh dưới 40.000đ cực hot của quán ạ:\n1. 🍑 [Trà Đào Cam Sả](http://localhost:3000/menu/tra-dao-cam-sa) - 39.000đ (Best-seller giải nhiệt)\n2. 🍊 [Trà Tắc Xí Muội Mật Ong](http://localhost:3000/menu/tra-tac-xi-muoi) - 32.000đ\n3. 🍉 [Nước Ép Dưa Hấu](http://localhost:3000/menu/nuoc-ep-dua-hau) - 38.000đ'}
        ]
    },
    # 9. Diet: sugar free
    {
        'messages': [
            {'role': 'system', 'content': SYS_PROMPT},
            {'role': 'user', 'content': 'mình đang ăn kiêng, muốn món không đường'},
            {'role': 'assistant', 'content': '', 'tool_calls': [{'name': 'search_menu', 'arguments': {'max_default_sugar_percent': 0}}]},
            {'role': 'tool', 'name': 'search_menu', 'content': json.dumps([
                {'id': 'MV-043', 'name_vi': 'Trà Oolong Nguyên Chất', 'price': 38000, 'tags': ['khong_duong', 'it_calo']},
                {'id': 'MV-115', 'name_vi': 'Nước Ép Táo Cà Rốt', 'price': 42000, 'tags': ['khong_duong', 'tot_suc_khoe']}
            ], ensure_ascii=False)},
            {'role': 'assistant', 'content': 'Team Healthy ăn kiêng thì chọn ngay **Trà Oolong Nguyên Chất** hoặc **Nước Ép Táo Cà Rốt Nguyên Chất** nhé. Ngoài ra tất cả các dòng trà trái cây tại MilkyVerse đều có mức điều chỉnh 0% đường theo ý bạn ạ!'}
        ]
    },
    # 10. Out of stock / search item
    {
        'messages': [
            {'role': 'system', 'content': SYS_PROMPT},
            {'role': 'user', 'content': 'cho mình trà sữa Thái xanh'},
            {'role': 'assistant', 'content': '', 'tool_calls': [{'name': 'search_menu', 'arguments': {'query': 'Thái xanh'}}]},
            {'role': 'tool', 'name': 'search_menu', 'content': json.dumps([
                {'id': 'MV-011', 'name_vi': 'Trà Sữa Thái Xanh', 'available': False, 'price': 42000}
            ], ensure_ascii=False)},
            {'role': 'assistant', 'content': 'Dạ món Trà Sữa Thái Xanh hôm nay vừa tạm hết cốt trà mất rồi ạ! Bạn có muốn đổi qua [Trà Sữa Matcha Nhật](http://localhost:3000/menu/tra-sua-matcha) thơm bùi thanh mát tương đương không ạ?'}
        ]
    },
    # 11. DIY custom builder inquiry
    {
        'messages': [
            {'role': 'system', 'content': SYS_PROMPT},
            {'role': 'user', 'content': 'mình muốn tự pha chế thì làm sao'},
            {'role': 'assistant', 'content': '', 'tool_calls': [{'name': 'get_custom_builder_options', 'arguments': {}}]},
            {'role': 'tool', 'name': 'get_custom_builder_options', 'content': json.dumps({
                'builder_url': 'http://localhost:3000/diy',
                'steps': ['Chọn cốt trà', 'Chọn loại sữa', 'Chọn hương vị syrup', 'Mức đường đá', 'Topping']
            }, ensure_ascii=False)},
            {'role': 'assistant', 'content': 'Bạn hoàn toàn có thể tự tay sáng tạo ly trà mang dấu ấn riêng tại phòng lab [Tự Pha Chế MilkyVerse](/diy) nha! Bạn được tự do chọn:\n- 5 loại cốt trà mộc\n- 4 loại sữa (Sữa Dalat, Sữa đặc, Sữa yến mạch, Hạnh nhân)\n- 6 hương vị trái cây & sốt béo\n- Hơn 20 loại topping độc quyền!\nBạn có ý tưởng hương vị nào chưa, Milky gợi ý công thức cho bạn nhé?'}
        ]
    },
    # 12. Build custom drink link
    {
        'messages': [
            {'role': 'system', 'content': SYS_PROMPT},
            {'role': 'user', 'content': 'mình muốn tự pha: không sữa, ít ngọt, vị đào'},
            {'role': 'assistant', 'content': '', 'tool_calls': [{'name': 'build_custom_drink_link', 'arguments': {
                'base': 'tra_lai',
                'milk': 'khong_sua',
                'flavor': 'dao',
                'sugar_percent': 30
            }}]},
            {'role': 'tool', 'name': 'build_custom_drink_link', 'content': json.dumps({
                'link': 'http://localhost:3000/diy?base=tra_lai&milk=khong_sua&flavor=dao&sugar=30',
                'estimated_price': 33000
            }, ensure_ascii=False)},
            {'role': 'assistant', 'content': 'Milky đã pha chế sẵn công thức theo đúng sở thích của bạn: **Cốt Lục Trà Lài + Vị Đào Hoàng Kim + Không Sữa + 30% Đường**. Bạn bấm vào đây để xem và đặt món ngay nhé: [Mở Công Thức Tự Pha Của Bạn](http://localhost:3000/diy?base=tra_lai&milk=khong_sua&flavor=dao&sugar=30)'}
        ]
    },
    # 13. Order tracking
    {
        'messages': [
            {'role': 'system', 'content': SYS_PROMPT},
            {'role': 'user', 'content': 'đơn của mình tới đâu rồi'},
            {'role': 'assistant', 'content': '', 'tool_calls': [{'name': 'list_orders', 'arguments': {'user_id': 'u001'}}]},
            {'role': 'tool', 'name': 'list_orders', 'content': json.dumps({
                'orders': [
                    {'order_id': 'MV-DH1002', 'name': 'Trà đào cam sả (x2)', 'status': 'delivering', 'tracking_url': 'http://localhost:3000/orders'}
                ]
            }, ensure_ascii=False)},
            {'role': 'assistant', 'content': 'Đơn hàng **MV-DH1002** (2 ly Trà đào cam sả) của bạn hiện đang trên đường giao (delivering) rồi ạ! Bạn có thể xem lộ trình shipper tại [Trang Đơn Hàng](http://localhost:3000/orders) nhé!'}
        ]
    },
    # 14. Vague request
    {
        'messages': [
            {'role': 'system', 'content': SYS_PROMPT},
            {'role': 'user', 'content': 'đặt cho mình ly gì đó'},
            {'role': 'assistant', 'content': 'Hôm nay bạn đang thích gu như thế nào nè? Bạn muốn uống **Trà sữa béo ngậy trân châu**, **Trà trái cây chua ngọt thanh mát**, hay đang tìm đồ uống **không ngọt / ít calo** để Milky chọn chuẩn vị nhất cho bạn?'}
        ]
    },
    # 15. Complex multi-action: cancel & re-order
    {
        'messages': [
            {'role': 'system', 'content': SYS_PROMPT},
            {'role': 'user', 'content': 'huỷ đơn mới nhất, mình thấy ngọt quá, đổi món nào đỡ ngọt, đừng trà sữa'},
            {'role': 'assistant', 'content': '', 'tool_calls': [{'name': 'list_orders', 'arguments': {'user_id': 'u001', 'status': 'pending'}}]},
            {'role': 'tool', 'name': 'list_orders', 'content': json.dumps({'orders': [{'order_id': 'MV-DH1001', 'name': 'Trà sữa trân châu đường đen', 'total': 58000, 'status': 'pending'}]}, ensure_ascii=False)},
            {'role': 'assistant', 'content': 'Milky thấy đơn **MV-DH1001** (Trà sữa trân châu đường đen) đang chờ xử lý. Bạn xác nhận huỷ đơn này để chuyển sang món thanh nhẹ hơn nhé?'},
            {'role': 'user', 'content': 'ừ huỷ đi rồi tìm món khác cho mình'},
            {'role': 'assistant', 'content': '', 'tool_calls': [
                {'name': 'cancel_order', 'arguments': {'user_id': 'u001', 'order_id': 'MV-DH1001', 'reason': 'Đổi món ít ngọt'}},
                {'name': 'search_menu', 'arguments': {'exclude_milk_tea': True, 'max_default_sugar_percent': 30}}
            ]},
            {'role': 'tool', 'name': 'cancel_order', 'content': json.dumps({'success': True, 'order_id': 'MV-DH1001'}, ensure_ascii=False)},
            {'role': 'tool', 'name': 'search_menu', 'content': json.dumps([
                {'id': 'MV-027', 'name_vi': 'Trà Đào Cam Sả (30% đường)', 'price': 39000, 'product_url': 'http://localhost:3000/menu/tra-dao-cam-sa'},
                {'id': 'MV-044', 'name_vi': 'Trà Xanh Hoa Lài Không Đường', 'price': 35000, 'product_url': 'http://localhost:3000/menu/tra-xanh-hoa-lai'}
            ], ensure_ascii=False)},
            {'role': 'assistant', 'content': 'Đơn cũ **MV-DH1001** đã được huỷ an toàn rồi ạ! Milky gợi ý cho bạn 2 món thanh nhẹ, ít ngọt:\n1. 🍑 [Trà Đào Cam Sả](http://localhost:3000/menu/tra-dao-cam-sa) (39.000đ) - chọn mức 30% đường vị thanh nhẹ cực dễ uống.\n2. 🍵 [Trà Xanh Hoa Lài](http://localhost:3000/menu/tra-xanh-hoa-lai) (35.000đ) - 0% đường thuần khiết.\nBạn ưng món nào thì nhắn Milky lên đơn ngay nhé!'}
        ]
    },
    # 16. Out of domain (e.g. Pizza) with snack recommendation
    {
        'messages': [
            {'role': 'system', 'content': SYS_PROMPT},
            {'role': 'user', 'content': 'quán có bán pizza không'},
            {'role': 'assistant', 'content': 'Dạ MilkyVerse không phục vụ pizza ạ! Nhưng quán có thực đơn đồ ăn vặt cực hấp dẫn để bạn nhâm nhi cùng trà sữa như: **Bánh tráng trộn tôm thịt**, **Cá viên chiên ngũ sắc**, **Khoai tây chiên lắc phô mai**, **Chân gà sốt Thái**, và **Mì cay 7 cấp độ** đó ạ! Bạn có muốn thử qua các món này không nè?'}
        ]
    }
]

# 8. README.md
readme_content = f'''# Dataset Huấn Luyện AI Barista MilkyVerse

Bộ dataset chuyên biệt cho Chatbot / AI Barista của thương hiệu trà sữa & đồ ăn vặt **MilkyVerse**.
Được trích xuất và đồng bộ hóa trực tiếp từ cơ sở dữ liệu thực tế của hệ thống MilkyVerse.

## Thống Kê Tổng Quan
- **Tổng số món (Products):** {len(menu_list)} món (Đầy đủ cả Đồ Uống & Đồ Ăn Vặt)
- **Tổng số Topping:** {len(toppings_list)} loại topping độc quyền
- **Tổng số Danh Mục:** {len(categories)} danh mục sản phẩm
- **Hội thoại mẫu (Conversations):** {len(convs)} kịch bản đối thoại chuẩn Function-Calling / Agent SFT
- **Tự pha chế (DIY Custom Builder):** Đầy đủ cốt trà, loại sữa, syrup hương vị và combo mẫu

## Cấu Trúc Các File
| Tên File | Định Dạng | Mô Tả Chi Tiết |
|---|---|---|
| `menu.json` | JSON | Toàn bộ 213 món với đầy đủ: `id`, `slug`, `name_vi`, `category`, `sizes`, `tags`, `is_milk_tea`, `has_milk`, `has_caffeine`, `default_sugar_percent`, `product_url`, `order_url` (Mua Ngay). |
| `menu.csv` | CSV | Phiên bản bảng tính phẳng của toàn bộ thực đơn. |
| `toppings.json` | JSON | 23 loại topping: Trân châu hoàng kim, trân châu trắng, trân châu đường đen, kem cheese, phô mai kéo sợi... kèm giá và tag. |
| `custom_builder.json` | JSON | Dữ liệu các bước tự pha chế cho trang `/diy` và 3 combo pha chế thịnh hành. |
| `orders_sample.json` | JSON | Dữ liệu mẫu đơn hàng, chính sách huỷ đơn (chỉ cho phép trạng thái `pending`). |
| `tools.json` | JSON Schema | 7 Function Calling declarations: `search_menu`, `get_item_link`, `create_order`, `list_orders`, `cancel_order`, `get_custom_builder_options`, `build_custom_drink_link`. |
| `conversations.jsonl` | JSON Lines | 16 kịch bản hội thoại đa lượt chân thực có tool call và kết quả trả về, dùng để fine-tune hoặc few-shot prompting cho LLM. |

## Đường Dẫn & Tích Hợp Hệ Thống
- Xem chi tiết món: `http://localhost:3000/menu/{{slug}}`
- Mua ngay trực tiếp: `http://localhost:3000/checkout?item={{id}}&size=M`
- Tự pha chế DIY: `http://localhost:3000/diy`
- Theo dõi đơn hàng: `http://localhost:3000/orders`
- Backend API Barista: `backend/MilkyVerse.Api/Services/AIBaristaService.cs`
'''

# Write to all output destinations
for out_dir in OUTPUT_DIRS:
    # 1. menu.json
    with open(os.path.join(out_dir, 'menu.json'), 'w', encoding='utf-8') as f:
        json.dump(menu_list, f, ensure_ascii=False, indent=2)

    # 2. menu.csv
    with open(os.path.join(out_dir, 'menu.csv'), 'w', encoding='utf-8', newline='') as f:
        writer = csv.writer(f)
        writer.writerow(['id', 'slug', 'name_vi', 'category', 'category_name', 'product_type', 'is_milk_tea', 'has_milk', 'has_caffeine', 'default_sugar_percent', 'price_m', 'tags', 'product_url', 'order_url'])
        for m in menu_list:
            writer.writerow([
                m['id'], m['slug'], m['name_vi'], m['category'], m['category_name'], m['product_type'],
                m['is_milk_tea'], m['has_milk'], m['has_caffeine'], m['default_sugar_percent'],
                m['sizes'].get('M', m['sizes'].get('Tiêu chuẩn', 0)),
                ';'.join(m['tags']), m['product_url'], m['order_url']
            ])

    # 3. toppings.json
    with open(os.path.join(out_dir, 'toppings.json'), 'w', encoding='utf-8') as f:
        json.dump(toppings_list, f, ensure_ascii=False, indent=2)

    # 4. custom_builder.json
    with open(os.path.join(out_dir, 'custom_builder.json'), 'w', encoding='utf-8') as f:
        json.dump(custom_builder, f, ensure_ascii=False, indent=2)

    # 5. orders_sample.json
    with open(os.path.join(out_dir, 'orders_sample.json'), 'w', encoding='utf-8') as f:
        json.dump(orders_sample, f, ensure_ascii=False, indent=2)

    # 6. tools.json
    with open(os.path.join(out_dir, 'tools.json'), 'w', encoding='utf-8') as f:
        json.dump(tools, f, ensure_ascii=False, indent=2)

    # 7. conversations.jsonl
    with open(os.path.join(out_dir, 'conversations.jsonl'), 'w', encoding='utf-8') as f:
        for c in convs:
            f.write(json.dumps(c, ensure_ascii=False) + '\n')

    # 8. README.md
    with open(os.path.join(out_dir, 'README.md'), 'w', encoding='utf-8') as f:
        f.write(readme_content)

# Create ZIP archive
zip_target = 'dataset_tra_sua/tra_sua_dataset.zip'
with zipfile.ZipFile(zip_target, 'w', zipfile.ZIP_DEFLATED) as zf:
    for filename in ['menu.json', 'menu.csv', 'toppings.json', 'custom_builder.json', 'orders_sample.json', 'tools.json', 'conversations.jsonl', 'README.md']:
        file_path = os.path.join('dataset_tra_sua/tra_sua_dataset', filename)
        zf.write(file_path, arcname=f'tra_sua_dataset/{filename}')

# Also copy zip to dataset/
import shutil
shutil.copyfile(zip_target, 'dataset/tra_sua_dataset_milkyverse.zip')

print('Sync completed successfully!')
print(f'Total products exported: {len(menu_list)}')
print(f'Total toppings exported: {len(toppings_list)}')
print(f'Total conversations exported: {len(convs)}')
