import { modalController } from "@ionic/vue";
export const openModal = async (Modal: any, props: any) => {
  const modal = await modalController.create({
    component: Modal,
    componentProps: props,
  });

  modal.present();

  await modal.onWillDismiss();
};

export const confirm = () => modalController.dismiss("confirm");
