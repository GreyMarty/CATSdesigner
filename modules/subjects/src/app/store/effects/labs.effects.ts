import { switchMap } from 'rxjs/operators';
import {Injectable} from '@angular/core';
import { Actions, ofType, createEffect } from '@ngrx/effects';
import {Store} from '@ngrx/store';
import {map, mergeMap, withLatestFrom} from 'rxjs/operators';

import {IAppState} from '../state/app.state';
import {LabsRestService} from '../../services/labs/labs-rest.service';
import * as groupsSelectors  from '../selectors/groups.selectors';
import * as labsActions from '../actions/labs.actions';
import * as subjectSelectors from '../selectors/subject.selector';
import * as filesActions from '../actions/files.actions';
import * as catsActions from '../actions/cats.actions';
import { iif, of } from 'rxjs';
import { ScheduleService } from 'src/app/services/schedule.service';

@Injectable()
export class LabsEffects {

  constructor(
    private actions$: Actions,
    private store: Store<IAppState>,
    private scheduleService: ScheduleService,
    private rest: LabsRestService) {
  }

  schedule$ = createEffect(() => this.actions$.pipe(
    ofType(labsActions.loadLabsSchedule),
    withLatestFrom(this.store.select(subjectSelectors.getSubjectId),this.store.select(groupsSelectors.getCurrentGroupId)),
    switchMap(([_, subjectId, groupId]) => this.rest.getProtectionSchedule(subjectId, groupId)),
    mergeMap(({ labs, scheduleProtectionLabs }) => [labsActions.loadLabsSuccess({ labs }), labsActions.laodLabsScheduleSuccess({ scheduleProtectionLabs })])
    )
  );

  updateOrder$ = createEffect(() => this.actions$.pipe(
    ofType(labsActions.updateOrder),
    withLatestFrom(this.store.select(subjectSelectors.getSubjectId)),
    switchMap(([{ prevIndex, currentIndex }, subjectId]) => 
    this.rest.updateLabsOrder(subjectId, prevIndex, currentIndex))
  ), { dispatch: false });

  labs$ = createEffect(() => this.actions$.pipe(
    ofType(labsActions.loadLabs),
    switchMap(() => this.store.select(subjectSelectors.getSubjectId)),
    switchMap(subjectId => this.rest.getLabWork(subjectId).pipe(
      map(labs => labsActions.loadLabsSuccess({ labs }))
    ))
  ));

  deleteLab$ = createEffect(() => this.actions$.pipe(
    ofType(labsActions.deleteLab),
    withLatestFrom(this.store.select(subjectSelectors.getSubjectId)),
    switchMap(([{ id }, subjectId]) => this.rest.deleteLab({ id, subjectId }).pipe(
      switchMap((body) => [catsActions.showMessage({ body }), labsActions.loadLabs()])
    ))
  ));

  createLab$ = createEffect(() => this.actions$.pipe(
    ofType(labsActions.saveLab),
    withLatestFrom(this.store.select(subjectSelectors.getSubjectId)),
    switchMap(([{ lab }, subjectId]) => (lab.subjectId = subjectId, this.rest.saveLab(lab)).pipe(
      switchMap((body) => [catsActions.showMessage({ body }),labsActions.loadLabs()])
    ))
  ));

  createDateVisit$ = createEffect(() => this.actions$.pipe(
    ofType(labsActions.createDateVisit),
    withLatestFrom(this.store.select(subjectSelectors.getSubjectId)),
    switchMap(([{ obj }, subjectId]) => this.scheduleService.createLabDateVisit({ ...obj, subjectId }).pipe(
      map(() => labsActions.loadLabsSchedule())
    ))
  ));

  deleteDateVisit$ = createEffect(() => this.actions$.pipe(
    ofType(labsActions.deleteDateVisit),
    switchMap(({ id }) => this.scheduleService.deleteLabDateVisit(id).pipe(
      map(() => labsActions.loadLabsSchedule())
    ))
  ));

  setLabsVisitingDate$ = createEffect(() => this.actions$.pipe(
    ofType(labsActions.setLabsVisitingDate),
    switchMap(({ visiting }) => this.rest.setLabsVisitingDate(visiting).pipe(
      switchMap((body) => [catsActions.showMessage({ body}), labsActions.loadLabStudents()])
    ))
  ));

  loadLabStudent$ = createEffect(() => this.actions$.pipe(
    ofType(labsActions.loadLabStudents),
    withLatestFrom(
      this.store.select(subjectSelectors.getSubjectId),
      this.store.select(groupsSelectors.getCurrentGroupId)
    ),
    switchMap(([_, subjectId, groupId]) => this.rest.getMarksV2(subjectId, groupId).pipe(
      map(students => labsActions.setLabStudents({ students }))
    ))
  ));

  setLabMark$ = createEffect(() => this.actions$.pipe(
    ofType(labsActions.setLabMark),
    switchMap(({ labMark }) => this.rest.setLabsMark(labMark).pipe(
      switchMap((body) => [catsActions.showMessage({ body }),labsActions.loadLabStudents()])
    ))
  ));

  loadStudentsLabsFiles$ = createEffect(() => this.actions$.pipe(
    ofType(labsActions.loadStudentsLabsFiles),
    withLatestFrom(
      this.store.select(subjectSelectors.getSubjectId),
      this.store.select(groupsSelectors.getCurrentGroupId)
    ),
    switchMap(([_, subjectId, groupId]) => this.rest.getAllStudentFilesLab(subjectId, groupId).pipe(
      map(studentsLabsFiles => labsActions.loadStudentsLabsFilesSuccess({ studentsLabsFiles }))
    ))
  ));

  labsVisitingExcel$ = createEffect(() => this.actions$.pipe(
    ofType(labsActions.getVisitingExcel),
    withLatestFrom(
        this.store.select(subjectSelectors.getSubjectId), 
        this.store.select(groupsSelectors.getCurrentGroupId), 
        this.store.select(groupsSelectors.getCurrentGroupSubGroupIds)),
    switchMap(([_, subjectId, groupId, [subGroupOneId, subGroupTwoId]]) => this.rest.getVisitLabsExcel(subjectId, groupId, subGroupOneId, subGroupTwoId).pipe(
        map(response => filesActions.getExcelData({ response }))
    ))
  ));

  labsMarksExcel$ = createEffect(() => this.actions$.pipe(
    ofType(labsActions.getMarksExcel),
    withLatestFrom(this.store.select(subjectSelectors.getSubjectId), this.store.select(groupsSelectors.getCurrentGroupId)),
    switchMap(([_, subjectId, groupId]) => this.rest.getLabsMarksExcel(subjectId, groupId).pipe(
      map(response => filesActions.getExcelData({ response }))
    ))
  ));

  sendUserFile$ = createEffect(() => this.actions$.pipe(
    ofType(labsActions.sendUserFile),
    withLatestFrom(
      this.store.select(subjectSelectors.getSubjectId),
      this.store.select(subjectSelectors.getUserId)
    ),
    switchMap(([{ sendFile }, subjectId, userId]) => this.rest.sendUserFile({ ...sendFile, subjectId }).pipe(
      switchMap(body => [catsActions.showMessage({ body }), labsActions.sendUserFileSuccess({ userLabFile: body, isReturned: userId !== sendFile.userId })])
    ))
  ));

  sendUserLabFileSuccess$ = createEffect(() => this.actions$.pipe(
    ofType(labsActions.sendUserFileSuccess),
    map(({ userLabFile, isReturned }) => isReturned ? labsActions.returnLab({ labId: userLabFile.LabId, studentId: userLabFile.UserId }) : labsActions.cancelLab({ labId: userLabFile.LabId, studentId: userLabFile.UserId }))
  ));

  deleteUserLabFile$ = createEffect(() => this.actions$.pipe(
    ofType(labsActions.deleteUserLabFile),
    switchMap(({ userLabFileId, userId, labId }) => this.rest.deleteUserFile(userLabFileId).pipe(
      map(() => labsActions.loadStudentLabFiles({ userId, labId }))
    ))
  ))

  getLabsAsZip$ = createEffect(() => this.actions$.pipe(
    ofType(labsActions.getLabsAsZip),
    withLatestFrom(
      this.store.select(groupsSelectors.getCurrentGroupId),
      this.store.select(subjectSelectors.getSubjectId)
    ),
    switchMap(([_, groupId, subjectId]) => this.rest.getLabsZip(subjectId, groupId).pipe(
      map(response => filesActions.getZipData({ response }))
    ))
  ));

  checkJobProtections$ = createEffect(() => this.actions$.pipe(
    ofType(labsActions.checkJobProtections),
    withLatestFrom(
      this.store.select(subjectSelectors.getSubjectId),
      this.store.select(subjectSelectors.isTeacher)
    ),
    switchMap(([_, subjectId, isTeacher]) => iif(() => isTeacher, this.rest.hasJobProtections(subjectId), of([])).pipe(
      map(hasJobProtections => labsActions.setJobProtections({ hasJobProtections }))
    ))));

  getUserLabFiles$ = createEffect(() => this.actions$.pipe(
    ofType(labsActions.loadStudentLabFiles),
    withLatestFrom(this.store.select(subjectSelectors.getUserId)),
    switchMap(([{ userId, labId }, currentUserId]) => this.rest.getUserLabFiles(!!userId ? userId : currentUserId, labId).pipe(
      map(labFiles => labsActions.loadStudentLabFilesSuccess({ labFiles, studentId: !!userId ? userId : currentUserId, labId }))
    ))
  ));

  loadGroupJobProtection$ = createEffect(() => this.actions$.pipe(
    ofType(labsActions.loadGroupJobProtection),
    withLatestFrom(
      this.store.select(subjectSelectors.getSubjectId),
      this.store.select(groupsSelectors.getCurrentGroupId)
    ),
    switchMap(([_, subjectId, groupId]) => this.rest.getGroupJobProtection(subjectId, groupId).pipe(
      map(groupJobProtection => labsActions.loadGroupJobProtectionSuccess({ groupJobProtection }))
    ))
  ));

  loadStudentJobProtection$ = createEffect(() => this.actions$.pipe(
    ofType(labsActions.loadStudentJobProtection),
    withLatestFrom(
      this.store.select(subjectSelectors.getSubjectId),
      this.store.select(subjectSelectors.getUserId)
    ),
    switchMap(([{ studentId }, subjectId, userId]) => this.rest.getStudentJobProtection(subjectId, studentId ? studentId : userId).pipe(
      map(studentJobProtection => labsActions.loadStudentJobProtectionSuccess({ studentJobProtection }))
    ))
  ));

  receiveLab$ = createEffect(() => this.actions$.pipe(
    ofType(labsActions.receiveLab),
    switchMap(({ labId, studentId }) => this.rest.receiveLab(labId, studentId).pipe(
      map(() => labsActions.receiveLabSuccess({ labId, studentId }))
    ))
  ));

  cancelLab$ = createEffect(() => this.actions$.pipe(
    ofType(labsActions.cancelLab),
    switchMap(({ labId, studentId }) => this.rest.cancelLab(labId, studentId).pipe(
      map(() => labsActions.cancelLabSuccess({ labId, studentId }))
    ))
  ));

  returnLab$ = createEffect(() => this.actions$.pipe(
    ofType(labsActions.returnLab),
    switchMap(({ labId, studentId }) => this.rest.returnLab(labId, studentId).pipe(
      map(() => labsActions.returnLabSuccess({ labId, studentId }))
    ))
  ));
}

