from selenium import webdriver
from selenium.webdriver.chrome.service import Service
from selenium.webdriver.chrome.options import Options
from selenium.webdriver.common.by import By
import os
import sqlite3
import time

def get_db_path():
    app_data_path = os.environ['LOCALAPPDATA']
    return os.path.join(app_data_path, "TELEBLASTER_PRO", "teleblaster.db")

def automate_group_finding(keyword, pages, is_headless=True):
    print(f"Running with is_headless={is_headless}")  # Log nilai is_headless
    driver = None
    try:
        # Open browser
        base_path = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", "..", ".."))
        chrome_path = os.path.join(base_path, "drivers", "GoogleChromePortable", "GoogleChromePortable.exe")
        driver_path = os.path.join(base_path, "drivers", "chromedriver.exe")
        
        options = Options()
        options.binary_location = chrome_path
        options.add_argument("--disable-gpu")
        options.add_argument("--no-sandbox")
        options.add_argument("--disable-dev-shm-usage")
        options.add_argument("--disable-extensions")
        options.add_argument("--disable-software-rasterizer")
        options.add_argument("--disable-popup-blocking")
        options.add_argument("--disable-sync")
        options.add_argument("--no-first-run")
        options.add_argument("--enable-automation")
        options.add_argument("--remote-debugging-port=9222")
        options.add_argument("--app=https://www.google.com")
        
        if is_headless is True:
            options.add_argument("--headless")
        
        service = Service(driver_path)
        driver = webdriver.Chrome(service=service, options=options)
        print("Browser opened successfully.")
        driver.get("http://www.google.com")
        
        # Hapus data link sebelumnya sebelum scrap baru
        clear_database()

        # Start automation
        search_box = driver.find_element(By.NAME, "q")
        search_query = f"site:t.me/joinchat {keyword}"
        search_box.send_keys(search_query)
        search_box.submit()
        
        time.sleep(1)
        
        group_links = []

        def scrape_links():
            elements = driver.find_elements(By.XPATH, "//h3[@class='LC20lb MBeuO DKV0Md']")
            for element in elements:
                parent = element.find_element(By.XPATH, "./ancestor::a[1]")
                href = parent.get_attribute("href")
                if href and "t.me/joinchat" in href:
                    group_links.append(href)

        scrape_links()

        for _ in range(pages - 1):
            try:
                next_button = driver.find_element(By.ID, "pnnext")
                next_button.click()
                
                time.sleep(1)
                
                scrape_links()
            except Exception as e:
                print(f"Failed to navigate to the next page: {e}")
                return "Failed to navigate to the next page"

        save_to_database(group_links)
        
        for telegram_link in group_links:
            print(telegram_link)
        
        return "Scraping completed successfully"
    except Exception as e:
        print(f"An error occurred: {e}")
        return f"An error occurred: {e}"
    finally:
        # Close browser
        if driver is not None:
            try:
                driver.quit()
                print("Browser closed successfully.")
                return "Browser closed successfully"
            except Exception as e:
                print(f"Failed to close browser: {e}")
                return f"Failed to close browser: {e}"

def clear_database():
    try:
        db_path = get_db_path()
        conn = sqlite3.connect(db_path)
        cursor = conn.cursor()
        
        cursor.execute('DELETE FROM group_links')  # Hapus semua data link
        conn.commit()
        conn.close()
    except Exception as e:
        print(f"Failed to clear database: {e}")

def save_to_database(links):
    try:
        db_path = get_db_path()
        conn = sqlite3.connect(db_path)
        cursor = conn.cursor()
        
        for link in links:
            cursor.execute('INSERT INTO group_links (link) VALUES (?)', (link,))
        
        conn.commit()
        conn.close()
    except Exception as e:
        print(f"Failed to save to database: {e}")
