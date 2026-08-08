import pyautogui

def move_mouse_to(x: int, y: int) -> None:
    try:
        pyautogui.moveTo(x, y)
    except Exception as e:
        print(f"Error in move_mouse_to: {e}")

def click_at(x: int, y: int) -> None:
    try:
        pyautogui.click(x, y)
    except Exception as e:
        print(f"Error in click_at: {e}")
