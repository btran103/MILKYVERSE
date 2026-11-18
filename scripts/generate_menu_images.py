#!/usr/bin/env python3
r"""
MILKYVERSE Menu Image Batch Generator (213 items)
Uses the official Google GenAI Interactions API (gemini-nano-banana-2.1) with layout reference images:
  - FOOD:  Pure seamless white (#FFFFFF) 45-degree commercial studio shot on clean white ceramic plate/bowl + dipping bowl
  - DRINK: Clean pure white studio shot with clear takeaway cup + vibrant flavor-matched liquid splash & floating leaf/fruit garnish
  - COMBO: Clean pure white studio pairing of the combo's drink (with subtle splash) + plated food side-by-side

Automatically saves images to:
  D:\MILKYVERSE\frontend\public\images\menu\{slug}.jpg
and syncs ImageUrl in:
  - D:\MILKYVERSE\backend\MilkyVerse.Api\milkyverse_local.db
  - D:\MILKYVERSE\backend\MilkyVerse.Api\Data\DbInitializer.cs

Usage:
  pip install -U google-genai
  $env:GEMINI_API_KEY="YOUR_API_KEY"
  python scripts/generate_menu_images.py                    # Runs FOOD -> COMBO -> DRINK (skips existing)
  python scripts/generate_menu_images.py --only FOOD COMBO  # Only 50 Food & Combo items
  python scripts/generate_menu_images.py --only DRINK       # Only 163 Drink items
  python scripts/generate_menu_images.py --model gemini-3.1-flash-lite-image --workers 5
"""

import argparse
import base64
import concurrent.futures
import os
import re
import sqlite3
import sys
import threading
import time
from pathlib import Path

sys.stdout.reconfigure(encoding="utf-8")

ROOT_DIR = Path(__file__).resolve().parent.parent
DB_PATH = ROOT_DIR / "backend" / "MilkyVerse.Api" / "milkyverse_local.db"
SEED_PATH = ROOT_DIR / "backend" / "MilkyVerse.Api" / "Data" / "DbInitializer.cs"
MENU_IMG_DIR = ROOT_DIR / "frontend" / "public" / "images" / "menu"
REF_DIR = Path(__file__).resolve().parent / "ref_layouts"
DRINK_REF_PATH = REF_DIR / "drink_ref.jpg"
FOOD_REF_PATH = REF_DIR / "food_ref.jpg"

MENU_IMG_DIR.mkdir(parents=True, exist_ok=True)

# Authentic Vietnamese culinary visual specs for all 37 FOOD & 13 COMBO items
CUSTOM_VISUALS = {
    # --- 37 FOOD ---
    "ca-vien-chien-thap-cam-met-lon": "A round woven bamboo platter lined with neatly trimmed green banana leaf loaded with golden crispy Vietnamese fried fish balls, beef balls, shrimp balls, mini round sausages (hồ lô), sliced sausages, and crispy mini spring rolls, neatly garnished with fanned crisp cucumber slices and pickled carrot/radish on one edge, plus two small clean white ceramic dipping bowls (dark hoisin-chili sauce and bright red chili sauce) behind the platter.",
    "ca-vien-chien-truyen-thong": "A clean square white ceramic plate angled slightly, neatly piled with golden-brown puffed crispy Vietnamese fried fish balls (with 2 fish balls cleanly halved in the center showing bouncy white fish paste interior), fanned fresh green cucumber slices lined neatly along the back edge of the plate, and a small round white porcelain dipping bowl of bright red chili sauce positioned behind the plate.",
    "bo-vien-chien-gan-gion": "A clean white ceramic plate neatly piled with golden-brown fried Vietnamese beef tendon balls (2 balls cleanly sliced open in front showing juicy peppery beef interior with visible tendon chunks), fanned fresh cucumber slices along the back edge, and a small white dipping bowl of dark hoisin-chili sauce (tương đen) behind the plate.",
    "tom-vien-chien-gion-rum": "A clean white ceramic plate neatly filled with golden-pink crispy fried Vietnamese shrimp balls (2 balls halved showing juicy pink-white shrimp interior), fanned fresh cucumber slices neatly lining the back edge, and a small white dipping bowl of sweet red chili sauce behind the plate.",
    "muc-vien-chien-nong": "A clean white ceramic plate neatly piled with plump golden-white crispy fried Vietnamese squid balls (2 balls halved showing bright white bouncy squid interior), fanned fresh cucumber slices lining the back edge, and a small white dipping bowl of bright red chili sauce behind the plate.",
    "ho-lo-nuong-chien-gion": "A clean white ceramic plate neatly holding glossy reddish-brown caramelized Vietnamese grilled mini round pork sausages (hồ lô nướng) on short bamboo skewers, fanned fresh cucumber slices neatly arranged along the back edge, and a small white dipping bowl of red chili sauce behind the plate.",
    "pho-mai-vien-chien-gion-tan": "A clean white ceramic plate neatly piled with golden-brown crispy breaded fried cheese balls, with one cheese ball cleanly halved in front showing molten stretchy golden-white mozzarella cheese oozing out, small garnish of crisp lettuce/cucumber on the back edge, and a small white dipping bowl of sweet chili sauce behind the plate.",
    "chan-ga-rut-xuong-sot-thai-cay-gion": "A clean matte white ceramic bowl filled with glossy fiery red-orange Vietnamese boneless chicken feet tossed in spicy Thai sauce, visibly mixed with crisp green mango slices, sliced baby ambarella (cóc non), lemongrass rings, bright green kumquat halves, red chilies, and shredded kaffir lime leaves, plus a tiny white dish of green chili-lime salt beside it.",
    "chan-ga-ngam-sa-tac-truyen-thong": "A clean white ceramic bowl filled with crisp pale-golden Vietnamese pickled chicken feet with thinly sliced lemongrass, bright green kumquat halves, shredded kaffir lime leaves, and red chili rings, paired with a small clean white dipping bowl of green chili-lime salt (muối ớt xanh) behind it.",
    "canh-ga-chien-nuoc-mam-toi-ot": "A clean white oval ceramic plate neatly holding glossy caramelized mahogany-gold crispy fried chicken wings glazed in sticky garlic-chili fish sauce, topped with golden crispy fried garlic chips and chopped red chilies, fanned fresh cucumber slices along the back edge, and a small white dipping bowl behind.",
    "ga-lac-pho-mai-beo-ngay": "A clean white ceramic plate neatly mounded with golden crispy bite-sized fried chicken fillet chunks heavily coated in bright savory-orange Korean cheese powder, neat fresh cucumber slices along the back edge, and a small white dipping bowl of sweet chili sauce behind the plate.",
    "ga-popcorn-chien-gion": "A clean white ceramic dish neatly heaped with ultra-crispy golden craggy popcorn chicken bites, fresh crisp cucumber slices along the back edge, and a small white dipping bowl of creamy chili-mayo sauce behind the plate.",
    "khoai-tay-lac-pho-mai-dac-biet": "A clean white ceramic dish neatly piled high with crisp golden shoestring French fries generously dusted with vibrant orange-gold cheese seasoning powder, with a small clean white dipping bowl of bright red ketchup positioned behind the plate.",
    "khoai-tay-lac-rong-bien-thom-gion": "A clean white ceramic dish neatly piled with golden crispy French fries heavily tossed with fragrant dark green toasted nori seaweed flakes and sesame seeds, with a small white dipping bowl of creamy mayonnaise positioned behind the plate.",
    "khoai-lang-ken-nuoc-cot-dua": "A clean white ceramic plate neatly stacked with plump golden-brown oval Vietnamese crispy sweet potato cocoons (khoai lang kén) studded with black sesame seeds, one piece gently halved in front showing warm creamy yellow sweet potato interior, and a small white dipping bowl of red chili sauce behind.",
    "khoai-tay-chien-sot-pho-mai-tan-chay": "A clean white ceramic plate loaded with hot crispy golden French fries generously smothered in thick, glossy, velvety melted cheddar cheese sauce and lightly sprinkled with fine parsley.",
    "pho-mai-que-keo-soi-khong-lo": "A clean white ceramic plate neatly stacked with golden panko-crusted crispy fried mozzarella cheese sticks (phô mai que), with one stick cleanly pulled apart in front showcasing a dramatic thick gooey melted mozzarella cheese stretch, and two tiny white dipping bowls of ketchup and creamy butter-mayo behind.",
    "xuc-xich-loc-xoay-chien-gion": "A clean white ceramic plate holding two golden crispy spiral-cut German smoked sausages on wooden skewers drizzled neatly with zigzags of ketchup and mayonnaise, with fanned cucumber slices on the back edge and a small white dipping bowl of chili sauce behind.",
    "nem-chua-ran-ha-noi-chuan-vi": "A clean white plate lined with a neat square of green banana leaf holding golden panko-breaded crispy fried Hanoi fermented pork rolls (nem chua rán), accompanied by neat cucumber/jicama sticks on the edge and a small white dipping bowl of bright red Vietnamese chili sauce behind.",
    "ha-cao-tom-thit-chien-gion": "A clean white ceramic plate neatly arranged with golden-brown crispy deep-fried pleated shrimp and pork dumplings (há cảo chiên giòn) with blistered flaky wrappers, neat pickled carrot/cucumber garnish on the back edge, and a small white dipping bowl of dark soy-vinegar chili sauce behind.",
    "banh-trang-tron-milkyverse-dac-biet": "A clean wide white ceramic bowl heaped with vibrant authentic Vietnamese rice paper salad (bánh tráng trộn): glossy orange satay-coated soft rice paper strips mixed with shredded green mango, dried beef jerky, shredded chicken jerky, tiny pink dried shrimp, halved soft-boiled quail eggs, roasted peanuts, crispy shallots, fresh Vietnamese coriander (rau răm), and a fresh kumquat half on top.",
    "banh-trang-nuong-da-lat-trung-thit-xuc-xich": "A clean round white plate holding crispy charcoal-grilled Dalat Vietnamese pizza (bánh tráng nướng): round blistered crisp rice paper baked with golden egg, bright green scallions, minced pork, crispy shallots, sliced red sausage, dried shrimp, and neat zigzags of mayonnaise and red chili sauce, cut neatly into slices.",
    "banh-trang-cuon-bo-sot-me-tac": "A clean rectangular white ceramic plate neatly displaying bite-sized translucent Vietnamese rice paper rolls (bánh tráng cuốn) stuffed with dried beef, quail egg, green mango, and herbs, paired with two small white dipping bowls behind: rich yellow egg-butter mayo sauce (sốt bơ) and glossy dark tamarind sauce topped with crushed peanuts.",
    "trung-chen-nuong-pho-mai-xuc-xich-pate": "Two small rustic terracotta/white ceramic ramekins (trứng chén nướng) filled with golden baked quail eggs topped with melted mozzarella cheese, savory pate chunks, sliced sausage, sizzling green scallion oil, crispy fried shallots, and a tiny dollop of red chili sauce.",
    "trung-cut-lon-xao-me-chua-ngot": "A clean white ceramic bowl filled with peeled Vietnamese quail eggs glistening in glossy amber-brown sweet-and-sour tamarind glaze (trứng cút lộn xào me), topped generously with crushed roasted peanuts, crispy golden shallots, fresh Vietnamese coriander (rau răm), and red chili slices.",
    "mi-cay-han-quoc-7-cap-do-hai-san-thap-cam": "A clean black Korean earthenware hotpot (thố đá) on a neat black trivet filled with fiery red-orange spicy Korean noodle soup: springy curly noodles loaded with bright red whole shrimp, scored squid rings, tender rolled US beef slices, enoki mushrooms, purple cabbage, fish cakes, and sliced sausage.",
    "mi-cay-bo-my-cuon-nam-kim-cham": "A clean black Korean earthenware hotpot filled with rich spicy red kimchi noodle soup overflowing with tender marbled US beef slices, crisp white enoki mushrooms, springy Korean noodles, kimchi, cabbage, green scallions, and toasted sesame seeds.",
    "mi-tron-xuc-xich-pho-mai-trung-long-dao": "A clean white ceramic bowl of glossy spicy-savory stir-fried instant noodles (mì trộn Indomie) topped with a glistening sunny-side-up runny yolk egg, seared sliced sausages, melted mozzarella cheese pull, crispy fried shallots, and chopped green scallions.",
    "com-chien-bo-kim-chi-trung-op-la": "A clean white ceramic plate of vibrant reddish-orange Korean kimchi fried rice with tender stir-fried sliced US beef, crowned with a golden-edged sunny-side-up fried egg with glossy yolk, toasted sesame seeds and shredded seaweed, paired with a small clean white bowl of clear seaweed soup behind.",
    "com-chien-hai-san-hoang-kim": "A clean white ceramic plate holding a neat dome of fluffy golden-yellow salted-egg-yolk fried rice (cơm chiên hải sản hoàng kim) studded with plump pink shrimp, white squid rings, bright green peas, diced carrots, and chopped scallions, with a small white bowl of soy-chili sauce behind.",
    "kimbap-chien-xu-gion-rum": "A clean rectangular white ceramic plate neatly displaying thick sliced rounds of crispy panko-breaded fried Korean kimbap showing colorful cross-sections of rice, seaweed, pink crab stick, yellow radish, omelet, sausage, and green cucumber, neatly drizzled with ketchup and mayonnaise.",
    "long-non-nuong-sa-te-cay-gion-than-hoa": "A clean white ceramic plate lined with neat banana leaf holding charcoal-grilled Vietnamese pork small intestines glazed in reddish-gold spicy honey-satay marinade, cut into neat bite-sized pieces with fanned cucumber slices, herbs, and a small white dipping bowl of green chili-lime sauce behind.",
    "doi-sun-nuong-than-hoa-gion-rum": "A clean white ceramic plate lined with neat banana leaf holding golden-brown charcoal-grilled Vietnamese cartilage pork sausages (dồi sụn nướng) sliced diagonally to show the herb-flecked juicy interior with crunchy cartilage bits, with cucumber slices, basil, and a small white bowl of chili dipping sauce behind.",
    "sun-ga-chien-nuoc-mam-rang-muoi-hong-kong": "A clean white ceramic plate heaped with crispy golden fried chicken cartilage tossed with fragrant crispy shredded lemongrass, golden fried garlic flakes, crispy kaffir lime leaves, and sliced red chilies, with a small white dipping bowl of chili sauce behind.",
    "bap-xao-bo-tep-mo-hanh-pho-mai": "A clean white ceramic bowl filled with glossy golden sweet corn kernels stir-fried in rich butter with tiny rosy dried shrimp, fragrant green scallion oil, and melted stretchy mozzarella cheese strings on top.",
    "banh-gao-tokbokki-cay-ngot-sot-pho-mai": "A clean white ceramic shallow bowl of glossy crimson-red Korean tteokbokki: chewy cylindrical rice cakes, folded fish cakes, sliced sausages, and a halved boiled egg in rich spicy-sweet gochujang sauce blanketed with bubbling melted golden-white mozzarella cheese and toasted sesame seeds.",
    "kho-ga-la-chanh-cay-xe-thom-lung": "A clean white ceramic bowl neatly mounded with fluffy golden-amber shredded spicy Vietnamese dried chicken jerky (khô gà lá chanh) generously mixed with crisp bright-green dried kaffir lime leaves and whole dried red chilies, with a fresh lime wedge beside it.",
    # --- 13 COMBO ---
    "combo-tra-sua-va-ca-vien-chien": "1 clear iced takeaway cup of Traditional Milk Tea with shiny black boba pearls and a dynamic creamy milk-tea splash behind the cup, paired cleanly beside 1 white ceramic plate of golden crispy Vietnamese fried fish balls with fanned cucumber slices and a small white dipping bowl of dark hoisin-satay sauce.",
    "combo-tra-sua-va-khoai-tay-chien": "1 clear iced takeaway cup of vibrant emerald-green Thai Green Milk Tea with a dynamic green milk-tea splash behind the cup, paired cleanly beside 1 white ceramic dish of golden cheese-dusted French fries and a small white dipping bowl of red ketchup.",
    "combo-tra-sua-va-banh-trang-tron": "1 clear iced takeaway cup of rich amber Roasted Oolong Milk Tea with boba and a dynamic amber tea-milk splash behind it, paired cleanly beside 1 white ceramic bowl of colorful Vietnamese rice paper salad (bánh tráng trộn) with beef jerky, quail eggs, green mango, and herbs.",
    "combo-tra-trai-cay-va-chan-ga-sa-tac": "1 large clear iced takeaway cup of refreshing Peach Orange Lemongrass Tea with peach wedges, orange slice, and a vibrant golden-peach splash behind it, paired cleanly beside 1 white ceramic bowl of Vietnamese pickled chicken feet with lemongrass and green kumquat halves.",
    "combo-tra-sua-va-pho-mai-que": "1 clear iced takeaway cup of rich Chocolate Milk Tea with dark chocolate swirls and a dynamic cocoa-milk splash behind it, paired cleanly beside 1 white ceramic plate of 3 golden crispy fried mozzarella cheese sticks (one pulled apart with a cheese stretch) and small dipping bowls.",
    "combo-tra-sua-va-xuc-xich-chien": "1 clear iced takeaway cup of Milk Tea with amber golden boba pearls and a creamy golden splash behind it, paired cleanly beside 1 white ceramic plate of 2 crispy spiral-cut fried German sausages on skewers drizzled with ketchup and mayo.",
    "combo-nuoc-ep-va-do-an-vat": "1 clear iced takeaway cup of vibrant pastel-pink Fresh Pink Guava Juice with a fresh pink splash & guava slice behind it, paired cleanly beside 1 banana-leaf-lined white plate of crispy golden Hanoi fried pork rolls (nem chua rán), cucumber sticks, and red chili sauce.",
    "combo-do-uong-va-mi-cay": "1 black Korean earthenware hotpot of fiery red spicy seafood & US beef noodle soup with shrimp and enoki mushrooms, paired cleanly beside 1 clear iced takeaway cup of Black Tea Macchiato topped with thick white cheese foam and a subtle amber-cream splash.",
    "combo-do-uong-va-com-chien": "1 clean white plate of Korean Kimchi Fried Rice topped with a glistening sunny-side-up fried egg, paired cleanly beside 1 large clear iced takeaway cup of refreshing Kumquat Iced Tea brimming with green kumquat halves and a bright citrus splash.",
    "combo-mot-nguoi-solo-treat": "1 clear iced takeaway cup of signature Creamy Boba Milk Tea topped with silky cheese foam and a creamy splash behind it, paired cleanly beside 1 white ceramic plate of glossy grilled Vietnamese mini round pork sausages (hồ lô chiên) with fanned cucumber slices and chili sauce.",
    "combo-hai-nguoi-doi-ban-than": "2 clear iced takeaway milk tea cups (one brown sugar boba milk tea and one matcha milk tea with vibrant twin splashes behind them), paired cleanly beside 1 bamboo platter of assorted fried fish/beef/shrimp balls AND 1 white dish of golden cheese-shaken French fries.",
    "combo-nhom-ban-party-sieu-vui": "A grand commercial party combo on pure white background: 4 colorful large iced takeaway cafe drinks (boba milk tea, peach tea, matcha latte, strawberry tea with dynamic splashes behind), arranged neatly with 1 big bamboo platter of assorted fried fish balls, 1 white bowl of spicy Thai-sauce chicken feet, and 2 bowls of Vietnamese rice paper salad.",
    "combo-tu-chon-do-uong-va-do-an": "1 clear iced takeaway milk tea cup with black pearls and a dynamic creamy splash behind it, paired cleanly beside 1 white tasting plate of golden crispy popcorn chicken bites and French fries with small white dipping bowls.",
}


def build_prompt(item: dict) -> str:
    slug = item["slug"]
    name = item["name"]
    cat = item["cat"]
    desc = item["desc"]
    ptype = item["type"]

    if ptype == "FOOD":
        subject = CUSTOM_VISUALS.get(slug, f"{name}: {desc}")
        return (
            f"Follow the EXACT commercial studio product layout, camera angle (clean 45-degree high angle), "
            f"lighting, and PURE SEAMLESS SOLID WHITE BACKGROUND (#FFFFFF) of the provided food reference image.\n"
            f'Create a crisp 1:1 commercial menu product photo for the Vietnamese snack/dish: "{name}".\n'
            f"Dish details: {subject}\n"
            f"Rules: Pure solid #FFFFFF white background with ONLY a very soft minimal contact shadow beneath the dish/bowls. "
            f"Bright, even, appetizing studio lighting, vivid true-to-life colors, crisp textures. "
            f"NO wooden table, NO cafe interior, NO bokeh background, NO people, NO hands, NO text, NO logos, NO watermarks."
        )

    if ptype == "COMBO":
        subject = CUSTOM_VISUALS.get(slug, f"{name}: {desc}")
        return (
            f"Combine the EXACT commercial studio layouts of the two provided reference images on a PURE SEAMLESS SOLID WHITE BACKGROUND (#FFFFFF): "
            f"drink cups with vibrant dynamic flavor-matched liquid splashes (like reference 1) paired cleanly beside plated Vietnamese food on white ceramic dishes with dipping bowls at a 45-degree angle (like reference 2).\n"
            f'Create a crisp 1:1 commercial menu combo photo for: "{name}".\n'
            f"Combo contents: {subject}\n"
            f"Rules: Pure solid #FFFFFF white studio background with only subtle minimal contact shadows. "
            f"Balanced hero composition centered in the square frame, bright appetizing F&B menu photography. "
            f"NO wooden table, NO cafe interior, NO bokeh, NO hands, NO text, NO logos, NO watermarks."
        )

    # DRINK (163 items) — exact splash + clear takeaway cup commercial layout from drink_ref.jpg
    n_low = name.lower()
    cat_low = cat.lower()

    if "nóng" in n_low or slug == "espresso":
        cup_style = "a sleek minimalist white ceramic cafe cup (filled with steaming beverage) centered hero-style with dynamic warm aromatic swirls/splash accents behind"
    elif "đá xay" in n_low or "frappe" in n_low or "frappuccino" in n_low or "cốt dừa" in n_low:
        cup_style = "a single crystal-clear takeaway cup filled with frosty icy blended frappe topped with a tall swirl of whipped cream & flavor drizzle, surrounded by an energetic creamy icy splash bursting behind the cup"
    elif "soda" in n_low:
        cup_style = "a single crystal-clear takeaway cup filled with crystal ice cubes, sparkling effervescent colored soda gradient, fresh fruit slices, and a dramatic sparkling fizzy liquid splash with floating droplets & fruit pieces bursting behind the cup"
    elif "nước ép" in n_low:
        cup_style = "a single crystal-clear takeaway cup filled with vibrant fresh cold-pressed fruit juice and ice cubes, surrounded by an energetic glossy fresh fruit juice splash and floating fresh fruit slices/leaves bursting behind the cup"
    elif "trà trái cây" in cat_low or "trà nguyên chất" in cat_low or "nước chanh" in cat_low:
        cup_style = "a single crystal-clear takeaway cup filled with jewel-toned iced tea/refresher, crystal ice cubes, and visible fresh fruit pieces/herbs, surrounded by a vibrant translucent tea & fruit splash with floating fresh tea leaves & fruit wedges bursting behind the cup"
    elif "macchiato" in cat_low or "kem cheese" in n_low or "kem trứng" in n_low or "kem muối" in n_low:
        cup_style = "a single crystal-clear takeaway cup showing distinct drink layers topped with a thick glossy velvet cream/cheese foam cap, surrounded by a dynamic dual-tone tea & creamy white foam splash bursting behind the cup"
    elif "cà phê" in cat_low or "cà phê" in n_low or "bạc xỉu" in n_low or "latte" in n_low or "cappuccino" in n_low or "americano" in n_low or "mocha" in n_low:
        cup_style = "a single crystal-clear takeaway iced coffee cup showing rich espresso & creamy milk swirls over crystal ice, surrounded by a dynamic glossy espresso & milk splash with roasted coffee beans floating behind the cup"
    else:
        cup_style = "a single crystal-clear takeaway milk tea cup filled with creamy milk tea, crystal ice cubes, and clearly visible toppings (pearls, jelly, pudding, flan, etc. matching the recipe), surrounded by a dynamic glossy flavor-colored milk tea splash and fresh green tea leaves bursting behind the cup"

    return (
        f"Follow the EXACT commercial beverage studio poster layout, vibrant glossy liquid splash style, floating leaf/ingredient accents, "
        f"and PURE CLEAN WHITE BACKGROUND (#FFFFFF) of the provided drink reference image — EXCEPT feature ONE single hero cup centered (instead of 3 cups), without any yellow semicircle at the bottom.\n"
        f'Create a crisp 1:1 commercial F&B catalog photo for the Vietnamese drink: "{name}" (Category: {cat}).\n'
        f"Drink presentation: {cup_style}.\n"
        f'Exact recipe & visual cues from Vietnamese menu description: "{desc}".\n'
        f"Rules: Match the exact beverage color, layering, syrup streaks, foam cap, and toppings mentioned in the name and description. "
        f"Pure clean white #FFFFFF studio background, ultra-crisp commercial lighting, high gloss appetizing splash, soft minimal contact shadow beneath the cup. "
        f"NO wooden table, NO cafe interior, NO text, NO cup branding/logo, NO watermarks."
    )


def load_menu_items(only_types=None):
    conn = sqlite3.connect(str(DB_PATH))
    cur = conn.cursor()
    rows = cur.execute(
        """
        SELECT p.Id, p.Name, p.Slug, p.ProductType, c.Name, p.Description
        FROM Products p
        JOIN Categories c ON p.CategoryId = c.Id
        ORDER BY
            CASE p.ProductType
                WHEN 'FOOD' THEN 1
                WHEN 'COMBO' THEN 2
                WHEN 'DRINK' THEN 3
                ELSE 4
            END,
            p.Id
        """
    ).fetchall()
    conn.close()
    items = [
        {
            "id": r[0],
            "name": r[1],
            "slug": r[2],
            "type": r[3],
            "cat": r[4],
            "desc": r[5] or "",
        }
        for r in rows
    ]
    if only_types:
        allowed = {t.upper() for t in only_types}
        items = [x for x in items if x["type"].upper() in allowed]
    return items


def sync_db_and_seed():
    available = {}
    for p in MENU_IMG_DIR.iterdir():
        if p.is_file() and p.suffix.lower() in {".jpg", ".jpeg", ".png", ".webp"} and p.stat().st_size > 10000:
            available[p.stem] = f"/images/menu/{p.name}"

    conn = sqlite3.connect(str(DB_PATH))
    cur = conn.cursor()
    db_updated = 0
    for slug, url in available.items():
        cur.execute("UPDATE Products SET ImageUrl = ? WHERE Slug = ?", (url, slug))
        db_updated += cur.rowcount
    conn.commit()
    conn.close()

    raw = SEED_PATH.read_bytes()
    block_re = re.compile(rb"new Product\s*\{[\s\S]*?\}")
    slug_re = re.compile(rb'Slug\s*=\s*"([^"]+)"')
    img_re = re.compile(rb'(ImageUrl\s*=\s*")([^"]*)(")')
    seed_updated = 0

    def replace_block(m):
        nonlocal seed_updated
        block = m.group(0)
        sm = slug_re.search(block)
        if not sm:
            return block
        slug = sm.group(1).decode("ascii", errors="ignore")
        if slug not in available:
            return block
        new_url = available[slug].encode("ascii")
        new_block, count = img_re.subn(rb"\g<1>" + new_url + rb"\g<3>", block, count=1)
        if count > 0 and new_block != block:
            seed_updated += 1
        return new_block

    new_raw = block_re.sub(replace_block, raw)
    if new_raw != raw:
        SEED_PATH.write_bytes(new_raw)

    print(f"[SYNC] Local images: {len(available)} | DB rows updated: {db_updated} | DbInitializer.cs blocks updated: {seed_updated}", flush=True)


def main():
    parser = argparse.ArgumentParser(description="Batch-generate MILKYVERSE menu images via Gemini Interactions API")
    parser.add_argument("--model", default="gemini-nano-banana-2.1", help="Gemini image model (default: gemini-nano-banana-2.1; fast alternative: gemini-3.1-flash-lite-image)")
    parser.add_argument("--only", nargs="+", choices=["FOOD", "COMBO", "DRINK"], help="Filter product types (default: all, ordered FOOD -> COMBO -> DRINK)")
    parser.add_argument("--workers", type=int, default=4, help="Concurrent API workers (default: 4)")
    parser.add_argument("--limit", type=int, default=0, help="Optional max number of pending items to generate in this run (0 = all)")
    parser.add_argument("--overwrite", action="store_true", help="Regenerate even if {slug}.jpg already exists")
    parser.add_argument("--sync-only", action="store_true", help="Only sync existing images in frontend/public/images/menu/ to DB and DbInitializer.cs")
    args = parser.parse_args()

    if args.sync_only:
        sync_db_and_seed()
        return

    if not os.environ.get("GEMINI_API_KEY"):
        print("ERROR: GEMINI_API_KEY environment variable is not set.", flush=True)
        print('Set it in PowerShell first:  $env:GEMINI_API_KEY="AIza..."', flush=True)
        sys.exit(1)

    try:
        from google import genai
    except ImportError:
        print("ERROR: google-genai SDK is not installed. Run:  pip install -U google-genai", flush=True)
        sys.exit(1)

    drink_ref_b64 = base64.b64encode(DRINK_REF_PATH.read_bytes()).decode("utf-8") if DRINK_REF_PATH.exists() else None
    food_ref_b64 = base64.b64encode(FOOD_REF_PATH.read_bytes()).decode("utf-8") if FOOD_REF_PATH.exists() else None

    items = load_menu_items(args.only)
    pending = []
    skipped = 0
    for it in items:
        out_path = MENU_IMG_DIR / f"{it['slug']}.jpg"
        if not args.overwrite and out_path.exists() and out_path.stat().st_size > 10000:
            skipped += 1
        else:
            pending.append(it)

    if args.limit > 0:
        pending = pending[: args.limit]

    print(f"MILKYVERSE Menu Generator | model={args.model} | total={len(items)} | already_done={skipped} | pending={len(pending)} | workers={args.workers}", flush=True)
    if not pending:
        sync_db_and_seed()
        return

    client = genai.Client()
    lock = threading.Lock()
    done_count = 0
    failed_slugs = []

    def process_one(it):
        nonlocal done_count
        slug = it["slug"]
        ptype = it["type"]
        out_path = MENU_IMG_DIR / f"{slug}.jpg"
        prompt = build_prompt(it)

        input_parts = [{"type": "text", "text": prompt}]
        if ptype == "FOOD" and food_ref_b64:
            input_parts.append({"type": "image", "data": food_ref_b64, "mime_type": "image/jpeg"})
        elif ptype == "DRINK" and drink_ref_b64:
            input_parts.append({"type": "image", "data": drink_ref_b64, "mime_type": "image/jpeg"})
        elif ptype == "COMBO":
            if drink_ref_b64:
                input_parts.append({"type": "image", "data": drink_ref_b64, "mime_type": "image/jpeg"})
            if food_ref_b64:
                input_parts.append({"type": "image", "data": food_ref_b64, "mime_type": "image/jpeg"})

        for attempt in range(1, 4):
            try:
                interaction = client.interactions.create(
                    model=args.model,
                    input=input_parts,
                    response_format={
                        "type": "image",
                        "mime_type": "image/jpeg",
                        "aspect_ratio": "1:1",
                        "image_size": "1K",
                    },
                    store=False,
                )
                img = interaction.output_image
                if img and img.data:
                    out_path.write_bytes(base64.b64decode(img.data))
                    with lock:
                        done_count += 1
                        cur_n = done_count
                        print(f"[{cur_n}/{len(pending)}] OK  ({ptype:5s}) {slug}.jpg ({out_path.stat().st_size // 1024} KB)", flush=True)
                        if cur_n % 10 == 0:
                            sync_db_and_seed()
                    return True
                else:
                    raise RuntimeError("No output_image returned")
            except Exception as e:
                err_str = str(e)
                wait_s = min(60, 5 * (2 ** (attempt - 1)))
                print(f"[RETRY {attempt}/3] {slug}: {err_str[:140]} -> waiting {wait_s}s", flush=True)
                time.sleep(wait_s)

        with lock:
            failed_slugs.append(slug)
            print(f"[FAIL] {slug}", flush=True)
        return False

    with concurrent.futures.ThreadPoolExecutor(max_workers=args.workers) as pool:
        list(pool.map(process_one, pending))

    sync_db_and_seed()
    print(f"\nFINISHED: generated={done_count}, failed={len(failed_slugs)}, skipped={skipped}", flush=True)
    if failed_slugs:
        print("Failed slugs (re-run the script to retry only these):", ", ".join(failed_slugs), flush=True)


if __name__ == "__main__":
    main()
